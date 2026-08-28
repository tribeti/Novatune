using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.Services;

public class PlaylistStorageService
{
    private const int RefreshConcurrency = 2;
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<YoutubePlaylist> _playlists = [];
    private bool _isLoaded;
    private bool _refreshStarted;
    private Task _refreshTask = Task.CompletedTask;

    public IReadOnlyList<YoutubePlaylist> Playlists => _playlists;
    public Task RefreshTask => _refreshTask;

    public PlaylistStorageService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(localAppData, "Novatune");
        Directory.CreateDirectory(appFolder);
        _filePath = Path.Combine(appFolder, "playlists.json");
    }

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

            if (!File.Exists(_filePath))
            {
                _playlists = [];
                return;
            }

            try
            {
                string json = await File.ReadAllTextAsync(_filePath).ConfigureAwait(false);
                _playlists = JsonSerializer.Deserialize(json, PlaylistJsonContext.Default.ListYoutubePlaylist) ?? [];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load playlists: {ex.Message}");
                _playlists = [];
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await SaveInternalAsync().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SaveInternalAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(_playlists, PlaylistJsonContext.Default.ListYoutubePlaylist);
            await File.WriteAllTextAsync(_filePath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save playlists: {ex.Message}");
        }
    }

    public async Task AddAsync(YoutubePlaylist playlist)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            int index = _playlists.FindIndex(p => p.PlaylistId == playlist.PlaylistId);

            if (index >= 0)
                _playlists[index] = playlist;
            else
                _playlists.Add(playlist);

            await SaveInternalAsync().ConfigureAwait(false);
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
            _playlists.RemoveAll(p => p.PlaylistId == playlistId);
            await SaveInternalAsync().ConfigureAwait(false);
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
                await SaveInternalAsync().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private sealed record PlaylistRefreshTarget(string PlaylistId, string PlaylistUrl);
    private sealed record PlaylistRefreshResult(string PlaylistId, YoutubePlaylist? Playlist);
}
