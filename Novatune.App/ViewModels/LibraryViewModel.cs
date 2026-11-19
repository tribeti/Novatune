using CommunityToolkit.Mvvm.ComponentModel;
using Novatune.App.Models;
using Novatune.App.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Novatune.App.ViewModels;

/// <summary>
/// View model for the Library page: favourites and play history.
/// Owns the displayed collections and library actions; playback is delegated
/// to MediaViewModel, persistence to LibraryService.
/// </summary>
public partial class LibraryViewModel : BaseViewModel
{
    private const int RecentDisplayLimit = 60;

    private readonly LibraryService _libraryService;
    private readonly MediaViewModel _mediaViewModel;

    public ObservableCollection<LibraryEntry> Favorites { get; } = [];
    public ObservableCollection<LibraryEntry> Recent { get; } = [];

    [ObservableProperty]
    public partial bool HasFavorites { get; set; }

    public LibraryViewModel(LibraryService libraryService, MediaViewModel mediaViewModel)
    {
        _libraryService = libraryService;
        _mediaViewModel = mediaViewModel;

        // Track playback so "Recently Played" stays live while the page is materialised.
        _mediaViewModel.TrackStarted += OnTrackStarted;
    }

    private void OnTrackStarted(LibraryEntry entry)
    {
        RecordHistory(entry);
        AddToRecent(entry);
    }

    private void RecordHistory(LibraryEntry entry) => _libraryService.RecordHistory(entry);

    private void AddToRecent(LibraryEntry entry)
    {
        for (int i = Recent.Count - 1; i >= 0; i--)
        {
            if (Recent[i].Key == entry.Key)
                Recent.RemoveAt(i);
        }

        Recent.Insert(0, entry);

        while (Recent.Count > RecentDisplayLimit)
            Recent.RemoveAt(Recent.Count - 1);
    }

    /// <summary>Refreshes favourites and history from the database.</summary>
    public async Task LoadAsync()
    {
        await _libraryService.InitializeAsync();

        Favorites.Clear();
        foreach (var favorite in _libraryService.Favorites)
            Favorites.Add(favorite);

        Recent.Clear();
        foreach (var recent in await _libraryService.GetHistoryAsync(RecentDisplayLimit))
            Recent.Add(recent);

        HasFavorites = Favorites.Count > 0;
    }

    /// <summary>Replays a library entry now.</summary>
    public async Task PlayEntryAsync(LibraryEntry entry)
    {
        await _mediaViewModel.PlayFromLibraryAsync(entry);
    }

    public async Task ToggleFavoriteAsync(MediaItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.SourcePathOrUrl))
            return;

        if (_libraryService.IsFavorite(item.Kind, item.SourcePathOrUrl))
        {
            if (await _libraryService.RemoveFavoriteAsync(item.Kind, item.SourcePathOrUrl))
                item.IsFavorite = false;
        }
        else
        {
            if (await _libraryService.AddFavoriteAsync(LibraryEntry.FromTrack(item)))
                item.IsFavorite = true;
        }
    }

    public async Task RemoveFavoriteAsync(LibraryEntry? entry)
    {
        if (entry is null)
            return;

        if (await _libraryService.RemoveFavoriteAsync(entry.Kind, entry.SourcePathOrUrl))
            await LoadAsync();
    }

    public async Task AddFavoriteAsync(LibraryEntry? entry)
    {
        if (entry is null)
            return;

        if (await _libraryService.AddFavoriteAsync(entry))
            await LoadAsync();
    }
}
