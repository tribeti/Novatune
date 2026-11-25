using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.Services;

/// <summary>
/// In-memory facade over LibraryDatabase for favourites and play history.
/// Favorites are cached for fast Contains checks; history is written-through.
/// </summary>
public class LibraryService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<LibraryEntry> _favorites = [];
    private HashSet<string> _favoriteKeys = new(StringComparer.Ordinal);
    private bool _isLoaded;

    public IReadOnlyList<LibraryEntry> Favorites => _favorites;

    public async Task InitializeAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isLoaded)
                return;

            try
            {
                _favorites = await Task.Run(() =>
                {
                    LibraryDatabase.InitializeDatabase();
                    return LibraryDatabase.GetFavorites();
                }).ConfigureAwait(false);
                _favoriteKeys = _favorites.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load library from DB: {ex.Message}");
                _favorites = [];
                _favoriteKeys = new HashSet<string>(StringComparer.Ordinal);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #region Favorites

    public bool IsFavorite(SourceKind kind, string sourcePathOrUrl) =>
        _favoriteKeys.Contains(new LibraryEntry { Kind = kind, SourcePathOrUrl = sourcePathOrUrl }.Key);

    public async Task<bool> AddFavoriteAsync(LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            bool added = await Task.Run(() => LibraryDatabase.AddFavorite(entry)).ConfigureAwait(false);
            if (added)
            {
                _favorites.Add(entry);
                _favoriteKeys.Add(entry.Key);
            }
            return added;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to add favourite: {ex.Message}");
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RemoveFavoriteAsync(SourceKind kind, string sourcePathOrUrl)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            bool removed = await Task.Run(() => LibraryDatabase.RemoveFavorite(kind, sourcePathOrUrl)).ConfigureAwait(false);
            if (removed)
            {
                var key = new LibraryEntry { Kind = kind, SourcePathOrUrl = sourcePathOrUrl }.Key;
                _favorites.RemoveAll(f => f.Key == key);
                _favoriteKeys.Remove(key);
            }
            return removed;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove favourite: {ex.Message}");
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Play history

    public void RecordHistory(LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _ = Task.Run(async () =>
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                LibraryDatabase.AddHistory(entry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to record history: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    public async Task ClearHistoryAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(LibraryDatabase.ClearHistory).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to clear history: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<LibraryEntry>> GetHistoryAsync(int limit = 60)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return await Task.Run(() => LibraryDatabase.GetHistory(limit)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read history: {ex.Message}");
            return [];
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion
}
