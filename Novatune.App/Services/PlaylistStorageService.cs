using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Novatune.App.Services;

public class PlaylistStorageService
{
    private readonly string _filePath;
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

    public async Task SaveAsync()
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
        _playlists.RemoveAll(p => p.PlaylistId == playlist.PlaylistId);
        _playlists.Add(playlist);
        await SaveAsync().ConfigureAwait(false);
    }

    public async Task RemoveAsync(string playlistId)
    {
        _playlists.RemoveAll(p => p.PlaylistId == playlistId);
        await SaveAsync().ConfigureAwait(false);
    }
}
