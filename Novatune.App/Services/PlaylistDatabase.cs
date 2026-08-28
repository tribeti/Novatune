using Microsoft.Data.Sqlite;
using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Novatune.App.Services;

/// <summary>
/// Static data-access layer for playlist storage in SQLite.
/// Database file: %LocalAppData%\Novatune\novatune.db
///
/// Schema:
///   Playlists  — one row per playlist (PlaylistId PK)
///   Videos     — one row per video, ordered by Position, FK → PlaylistId
/// </summary>
public static class PlaylistDatabase
{
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
    /// Creates the database file and tables if they don't already exist.
    /// Should be called once at app startup.
    /// </summary>
    public static void InitializeDatabase()
    {
        using var db = CreateConnection();
        db.Open();

        // Enable WAL mode for better concurrent read/write performance
        using (var walCmd = new SqliteCommand("PRAGMA journal_mode=WAL;", db))
            walCmd.ExecuteNonQuery();

        // Enable foreign key enforcement
        using (var fkCmd = new SqliteCommand("PRAGMA foreign_keys=ON;", db))
            fkCmd.ExecuteNonQuery();

        const string createPlaylists =
            """
            CREATE TABLE IF NOT EXISTS Playlists (
                PlaylistId   TEXT PRIMARY KEY,
                Title        TEXT NOT NULL,
                Author       TEXT NOT NULL,
                ThumbnailUrl TEXT NOT NULL,
                PlaylistUrl  TEXT NOT NULL
            );
            """;

        const string createVideos =
            """
            CREATE TABLE IF NOT EXISTS Videos (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                PlaylistId   TEXT    NOT NULL REFERENCES Playlists(PlaylistId) ON DELETE CASCADE,
                Position     INTEGER NOT NULL,
                Title        TEXT    NOT NULL,
                Author       TEXT    NOT NULL,
                VideoUrl     TEXT    NOT NULL,
                ThumbnailUrl TEXT    NOT NULL
            );
            """;

        using (var cmd = new SqliteCommand(createPlaylists, db))
            cmd.ExecuteNonQuery();

        using (var cmd = new SqliteCommand(createVideos, db))
            cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns all playlists with their associated videos, ordered by Position.
    /// </summary>
    public static List<YoutubePlaylist> GetAllPlaylists()
    {
        var playlists = new Dictionary<string, YoutubePlaylist>(StringComparer.Ordinal);
        var order = new List<string>();

        using var db = CreateConnection();
        db.Open();

        const string sql =
            """
            SELECT p.PlaylistId, p.Title, p.Author, p.ThumbnailUrl, p.PlaylistUrl,
                   v.Title AS VideoTitle, v.Author AS VideoAuthor,
                   v.VideoUrl, v.ThumbnailUrl AS VideoThumbnailUrl
            FROM Playlists p
            LEFT JOIN Videos v ON v.PlaylistId = p.PlaylistId
            ORDER BY p.rowid, v.Position;
            """;

        using var cmd = new SqliteCommand(sql, db);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            string playlistId = reader.GetString(0);

            if (!playlists.TryGetValue(playlistId, out var playlist))
            {
                playlist = new YoutubePlaylist
                {
                    PlaylistId = playlistId,
                    Title = reader.GetString(1),
                    Author = reader.GetString(2),
                    ThumbnailUrl = reader.GetString(3),
                    PlaylistUrl = reader.GetString(4),
                };
                playlists[playlistId] = playlist;
                order.Add(playlistId);
            }

            if (!reader.IsDBNull(5))
            {
                playlist.Videos.Add(new YoutubeItem
                {
                    Title = reader.GetString(5),
                    Author = reader.GetString(6),
                    VideoUrl = reader.GetString(7),
                    ThumbnailUrl = reader.GetString(8),
                });
            }
        }

        var result = new List<YoutubePlaylist>(order.Count);
        foreach (var id in order)
            result.Add(playlists[id]);

        return result;
    }

    /// <summary>
    /// Inserts or replaces a playlist and all of its videos atomically.
    /// </summary>
    public static void UpsertPlaylist(YoutubePlaylist playlist)
    {
        using var db = CreateConnection();
        db.Open();

        using var fkCmd = new SqliteCommand("PRAGMA foreign_keys=ON;", db);
        fkCmd.ExecuteNonQuery();

        using var transaction = db.BeginTransaction();

        try
        {
            using (var cmd = new SqliteCommand(
                """
                INSERT INTO Playlists (PlaylistId, Title, Author, ThumbnailUrl, PlaylistUrl)
                VALUES (@PlaylistId, @Title, @Author, @ThumbnailUrl, @PlaylistUrl)
                ON CONFLICT(PlaylistId) DO UPDATE SET
                    Title        = excluded.Title,
                    Author       = excluded.Author,
                    ThumbnailUrl = excluded.ThumbnailUrl,
                    PlaylistUrl  = excluded.PlaylistUrl;
                """, db, transaction))
            {
                cmd.Parameters.AddWithValue("@PlaylistId", playlist.PlaylistId);
                cmd.Parameters.AddWithValue("@Title", playlist.Title);
                cmd.Parameters.AddWithValue("@Author", playlist.Author);
                cmd.Parameters.AddWithValue("@ThumbnailUrl", playlist.ThumbnailUrl);
                cmd.Parameters.AddWithValue("@PlaylistUrl", playlist.PlaylistUrl);
                cmd.ExecuteNonQuery();
            }

            using (var deleteCmd = new SqliteCommand("DELETE FROM Videos WHERE PlaylistId = @PlaylistId;", db, transaction))
            {
                deleteCmd.Parameters.AddWithValue("@PlaylistId", playlist.PlaylistId);
                deleteCmd.ExecuteNonQuery();
            }

            using var insertCmd = new SqliteCommand(
                """
                INSERT INTO Videos (PlaylistId, Position, Title, Author, VideoUrl, ThumbnailUrl)
                VALUES (@PlaylistId, @Position, @Title, @Author, @VideoUrl, @ThumbnailUrl);
                """, db, transaction);

            var pPlaylistId = insertCmd.Parameters.Add("@PlaylistId", SqliteType.Text);
            var pPosition = insertCmd.Parameters.Add("@Position", SqliteType.Integer);
            var pTitle = insertCmd.Parameters.Add("@Title", SqliteType.Text);
            var pAuthor = insertCmd.Parameters.Add("@Author", SqliteType.Text);
            var pVideoUrl = insertCmd.Parameters.Add("@VideoUrl", SqliteType.Text);
            var pThumb = insertCmd.Parameters.Add("@ThumbnailUrl", SqliteType.Text);

            for (int i = 0; i < playlist.Videos.Count; i++)
            {
                var video = playlist.Videos[i];
                pPlaylistId.Value = playlist.PlaylistId;
                pPosition.Value = i;
                pTitle.Value = video.Title;
                pAuthor.Value = video.Author;
                pVideoUrl.Value = video.VideoUrl;
                pThumb.Value = video.ThumbnailUrl;
                insertCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Deletes a playlist and all its videos (CASCADE).
    /// </summary>
    public static void DeletePlaylist(string playlistId)
    {
        using var db = CreateConnection();
        db.Open();

        using var fkCmd = new SqliteCommand("PRAGMA foreign_keys=ON;", db);
        fkCmd.ExecuteNonQuery();

        using var cmd = new SqliteCommand("DELETE FROM Playlists WHERE PlaylistId = @PlaylistId;", db);

        cmd.Parameters.AddWithValue("@PlaylistId", playlistId);
        cmd.ExecuteNonQuery();
    }
}
