using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.Services;

public class PlaylistStorageService
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<YoutubePlaylist> _playlists = [];

    public IReadOnlyList<YoutubePlaylist> Playlists => _playlists;

    public PlaylistStorageService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(localAppData, "Novatune");
        Directory.CreateDirectory(appFolder);
        _filePath = Path.Combine(appFolder, "playlists.json");
    }

    public async Task LoadAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
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
            _playlists.RemoveAll(p => p.PlaylistId == playlist.PlaylistId);
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
}
