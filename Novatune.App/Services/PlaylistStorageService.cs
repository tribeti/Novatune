using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.Services;

public class PlaylistStorageService
{
    private const int RefreshConcurrency = 2;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<YoutubePlaylist> _playlists = [];
    private bool _isLoaded;
    private bool _refreshStarted;
    private Task _refreshTask = Task.CompletedTask;

    public IReadOnlyList<YoutubePlaylist> Playlists => _playlists;
    public Task RefreshTask => _refreshTask;

    public async Task InitializeAsync()
    {
        await LoadAsync().ConfigureAwait(false);

        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_refreshStarted)
                return;

            _refreshStarted = true;

            var snapshot = _playlists
                .Select(p => new PlaylistRefreshTarget(p.PlaylistId, p.PlaylistUrl))
                .Where(p => !string.IsNullOrWhiteSpace(p.PlaylistUrl))
                .ToList();

            _refreshTask = RefreshAllFromYouTubeAsync(snapshot);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task LoadAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isLoaded)
                return;

            _isLoaded = true;

            try
            {
                _playlists = await Task.Run(PlaylistDatabase.GetAllPlaylists).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load playlists from DB: {ex.Message}");
                _playlists = [];
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task SaveAsync() => Task.CompletedTask;

    public async Task AddAsync(YoutubePlaylist playlist)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(() => PlaylistDatabase.UpsertPlaylist(playlist)).ConfigureAwait(false);
            int index = _playlists.FindIndex(p => p.PlaylistId == playlist.PlaylistId);
            if (index >= 0)
                _playlists[index] = playlist;
            else
                _playlists.Add(playlist);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to add playlist to DB: {ex.Message}");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveAsync(string playlistId)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(() => PlaylistDatabase.DeletePlaylist(playlistId)).ConfigureAwait(false);
            _playlists.RemoveAll(p => p.PlaylistId == playlistId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove playlist from DB: {ex.Message}");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshAllFromYouTubeAsync(IReadOnlyList<PlaylistRefreshTarget> snapshot)
    {
        if (snapshot.Count == 0)
            return;

        using var throttler = new SemaphoreSlim(RefreshConcurrency, RefreshConcurrency);

        var refreshTasks = snapshot.Select(async target =>
        {
            await throttler.WaitAsync().ConfigureAwait(false);
            try
            {
                var refreshedPlaylist = await YoutubeService.GetPlaylistAsync(target.PlaylistUrl)
                    .ConfigureAwait(false);

                return new PlaylistRefreshResult(target.PlaylistId, refreshedPlaylist);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Failed to refresh playlist {target.PlaylistId}: {ex.Message}");

                return new PlaylistRefreshResult(target.PlaylistId, null);
            }
            finally
            {
                throttler.Release();
            }
        });

        PlaylistRefreshResult[] results = await Task.WhenAll(refreshTasks).ConfigureAwait(false);

        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            bool changed = false;

            foreach (var result in results)
            {
                if (result.Playlist is null)
                    continue;

                int index = _playlists.FindIndex(p => p.PlaylistId == result.PlaylistId);
                if (index < 0)
                    continue;

                _playlists[index] = result.Playlist;
                changed = true;
            }

            if (changed)
            {
                var toUpsert = results
                    .Where(r => r.Playlist is not null)
                    .Select(r => r.Playlist!);

                await Task.Run(() =>
                {
                    foreach (var p in toUpsert)
                        PlaylistDatabase.UpsertPlaylist(p);
                }).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private sealed record PlaylistRefreshTarget(string PlaylistId, string PlaylistUrl);
    private sealed record PlaylistRefreshResult(string PlaylistId, YoutubePlaylist? Playlist);
}
