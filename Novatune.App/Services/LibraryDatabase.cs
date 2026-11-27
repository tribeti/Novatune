using Microsoft.Data.Sqlite;
using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Novatune.App.Services;

/// <summary>
/// Static data-access layer for the media library (favourites + play history)
/// stored in the same SQLite file as playlists: %LocalAppData%\Novatune\novatune.db
///
/// Schema:
///   Favorites   — one row per favourited media (unique by Kind + SourcePathOrUrl)
///   PlayHistory — most recent plays, capped at HistoryCap rows
/// </summary>
public static class LibraryDatabase
{
    /// <summary>Maximum number of play-history rows kept.</summary>
    public const int HistoryCap = 50;

    private static readonly string _dbPath = BuildDbPath();

    private static string BuildDbPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(localAppData, "Novatune");
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, "novatune.db");
    }

    private static SqliteConnection CreateConnection() => new($"Filename={_dbPath}");

    /// <summary>
    /// Creates the library tables if they don't already exist.
    /// Idempotent; safe to call on every app start alongside PlaylistDatabase.InitializeDatabase.
    /// </summary>
    public static void InitializeDatabase()
    {
        using var db = CreateConnection();
        db.Open();

        using (var fkCmd = new SqliteCommand("PRAGMA foreign_keys=ON;", db))
            fkCmd.ExecuteNonQuery();

        const string createFavorites =
            """
            CREATE TABLE IF NOT EXISTS Favorites (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                Kind           INTEGER NOT NULL,
                Title          TEXT    NOT NULL,
                Subtitle       TEXT    NOT NULL,
                SourcePathOrUrl TEXT   NOT NULL,
                ThumbnailUrl   TEXT    NOT NULL,
                SavedAt        TEXT    NOT NULL,
                UNIQUE (Kind, SourcePathOrUrl)
            );
            """;

        const string createHistory =
            """
            CREATE TABLE IF NOT EXISTS PlayHistory (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                Kind           INTEGER NOT NULL,
                Title          TEXT    NOT NULL,
                Subtitle       TEXT    NOT NULL,
                SourcePathOrUrl TEXT   NOT NULL,
                ThumbnailUrl   TEXT    NOT NULL,
                PlayedAt       TEXT    NOT NULL
            );
            """;

        using (var cmd = new SqliteCommand(createFavorites, db))
            cmd.ExecuteNonQuery();

        using (var cmd = new SqliteCommand(createHistory, db))
            cmd.ExecuteNonQuery();

        using var dedupeCmd = new SqliteCommand(
            """
            DELETE FROM PlayHistory WHERE Id NOT IN (
                SELECT MAX(Id) FROM PlayHistory GROUP BY Kind, SourcePathOrUrl
            );
            """, db);
        dedupeCmd.ExecuteNonQuery();
    }

    #region Favorites

    public static List<LibraryEntry> GetFavorites()
    {
        var result = new List<LibraryEntry>();

        using var db = CreateConnection();
        db.Open();

        using var cmd = new SqliteCommand(
            """
            SELECT Kind, Title, Subtitle, SourcePathOrUrl, ThumbnailUrl, SavedAt
            FROM Favorites
            ORDER BY rowid;
            """, db);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadEntry(reader));

        return result;
    }

    public static bool AddFavorite(LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using var db = CreateConnection();
        db.Open();

        using var cmd = new SqliteCommand(
            """
            INSERT INTO Favorites (Kind, Title, Subtitle, SourcePathOrUrl, ThumbnailUrl, SavedAt)
            VALUES (@Kind, @Title, @Subtitle, @SourcePathOrUrl, @ThumbnailUrl, @SavedAt)
            ON CONFLICT(Kind, SourcePathOrUrl) DO NOTHING;
            """, db);

        BindEntry(cmd, entry);
        return cmd.ExecuteNonQuery() > 0;
    }

    public static bool RemoveFavorite(SourceKind kind, string sourcePathOrUrl)
    {
        using var db = CreateConnection();
        db.Open();

        using var cmd = new SqliteCommand("DELETE FROM Favorites WHERE Kind = @Kind AND SourcePathOrUrl = @SourcePathOrUrl;", db);

        cmd.Parameters.AddWithValue("@Kind", (int) kind);
        cmd.Parameters.AddWithValue("@SourcePathOrUrl", sourcePathOrUrl ?? string.Empty);
        return cmd.ExecuteNonQuery() > 0;
    }

    #endregion

    #region Play history

    /// <summary>
    /// Records a play. Keeps at most one row per media: a replay removes the old
    /// row and re-inserts at the top with a fresh PlayedAt, then trims to HistoryCap.
    /// </summary>
    public static void AddHistory(LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using var db = CreateConnection();
        db.Open();

        using (var cmd = new SqliteCommand(
            """
            DELETE FROM PlayHistory WHERE Kind = @Kind AND SourcePathOrUrl = @SourcePathOrUrl;
            INSERT INTO PlayHistory (Kind, Title, Subtitle, SourcePathOrUrl, ThumbnailUrl, PlayedAt)
            VALUES (@Kind, @Title, @Subtitle, @SourcePathOrUrl, @ThumbnailUrl, @PlayedAt);
            """, db))
        {
            BindEntry(cmd, entry, timestampName: "@PlayedAt");
            cmd.ExecuteNonQuery();
        }

        using (var trimCmd = new SqliteCommand(
            """
            DELETE FROM PlayHistory WHERE Id NOT IN (
                SELECT Id FROM PlayHistory ORDER BY PlayedAt DESC, Id DESC LIMIT @Cap
            );
            """, db))
        {
            trimCmd.Parameters.AddWithValue("@Cap", HistoryCap);
            trimCmd.ExecuteNonQuery();
        }
    }

    /// <summary>Deletes the play-history rows for one media.</summary>
    public static void RemoveHistory(SourceKind kind, string sourcePathOrUrl)
    {
        using var db = CreateConnection();
        db.Open();

        using var cmd = new SqliteCommand(
            "DELETE FROM PlayHistory WHERE Kind = @Kind AND SourcePathOrUrl = @SourcePathOrUrl;", db);
        cmd.Parameters.AddWithValue("@Kind", (int) kind);
        cmd.Parameters.AddWithValue("@SourcePathOrUrl", sourcePathOrUrl);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Deletes every play-history row.</summary>
    public static void ClearHistory()
    {
        using var db = CreateConnection();
        db.Open();

        using var cmd = new SqliteCommand("DELETE FROM PlayHistory;", db);
        cmd.ExecuteNonQuery();
    }

    public static List<LibraryEntry> GetHistory(int limit = 60)
    {
        var result = new List<LibraryEntry>();

        using var db = CreateConnection();
        db.Open();

        using var cmd = new SqliteCommand(
            """
            SELECT Kind, Title, Subtitle, SourcePathOrUrl, ThumbnailUrl, PlayedAt
            FROM PlayHistory
            ORDER BY PlayedAt DESC, Id DESC
            LIMIT @Limit;
            """, db);

        cmd.Parameters.AddWithValue("@Limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadEntry(reader));

        return result;
    }

    #endregion

    #region Helpers

    private static LibraryEntry ReadEntry(SqliteDataReader reader)
    {
        return new LibraryEntry
        {
            Kind = (SourceKind) reader.GetInt32(0),
            Title = reader.GetString(1),
            Subtitle = reader.GetString(2),
            SourcePathOrUrl = reader.GetString(3),
            ThumbnailUrl = reader.GetString(4),
            SavedAt = DateTime.TryParse(reader.GetString(5), out var ts) ? ts : DateTime.UtcNow,
        };
    }

    private static void BindEntry(SqliteCommand cmd, LibraryEntry entry, string timestampName = "@SavedAt")
    {
        cmd.Parameters.AddWithValue("@Kind", (int) entry.Kind);
        cmd.Parameters.AddWithValue("@Title", entry.Title);
        cmd.Parameters.AddWithValue("@Subtitle", entry.Subtitle);
        cmd.Parameters.AddWithValue("@SourcePathOrUrl", entry.SourcePathOrUrl);
        cmd.Parameters.AddWithValue("@ThumbnailUrl", entry.ThumbnailUrl);
        cmd.Parameters.AddWithValue(timestampName, entry.SavedAt.ToString("O"));
    }

    #endregion
}
