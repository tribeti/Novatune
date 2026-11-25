using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Novatune.App.Models;
using Novatune.App.Services;
using Novatune.App.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Novatune.App.Views;

public sealed partial class LibraryPage : Page
{
    public ObservableCollection<YoutubePlaylist> OnlinePlaylistItems { get; } = [];
    public LibraryViewModel Vm { get; }

    private readonly MediaViewModel ViewModel = App.Current.Services.GetRequiredService<MediaViewModel>();
    private readonly PlaylistStorageService _playlistStorage = App.Current.Services.GetRequiredService<PlaylistStorageService>();
    private bool _isRefreshingPlaylist;

    public LibraryPage()
    {
        InitializeComponent();
        Vm = App.Current.Services.GetRequiredService<LibraryViewModel>();
        LoadPlaylists();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = Vm.LoadAsync();
    }

    private async void LoadPlaylists()
    {
        try
        {
            await _playlistStorage.InitializeAsync();
            RefreshPlaylistItems();

            await _playlistStorage.RefreshTask;
            RefreshPlaylistItems();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load playlists: {ex.Message}");
        }
    }

    private void RefreshPlaylistItems()
    {
        OnlinePlaylistItems.Clear();

        foreach (var playlist in _playlistStorage.Playlists)
        {
            OnlinePlaylistItems.Add(playlist);
        }
    }

    private async void AddPlaylist_Click(object sender, RoutedEventArgs e)
    {
        var url = await DialogService.ShowInputAsync(
            Strings.Get("Dialog_ImportPlaylistTitle"),
            "https://youtube.com/playlist?list=...",
            Strings.Get("Dialog_Import"),
            Strings.Get("Dialog_Cancel"));
        if (url is null)
            return;

        var loading = DialogService.ShowLoading(Strings.Get("Dialog_ImportingPlaylist"));

        try
        {
            var playlist = await YoutubeService.GetPlaylistAsync(url);
            await loading.HideAsync();

            if (playlist is null)
            {
                await DialogService.ShowErrorAsync(Strings.Get("Dialog_FailedToLoadPlaylist"));
                return;
            }

            await _playlistStorage.AddAsync(playlist);
            for (int i = OnlinePlaylistItems.Count - 1; i >= 0; i--)
            {
                if (OnlinePlaylistItems[i].PlaylistId == playlist.PlaylistId)
                    OnlinePlaylistItems.RemoveAt(i);
            }
            OnlinePlaylistItems.Add(playlist);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Import playlist failed: {ex.Message}");
            await loading.HideAsync();
            await DialogService.ShowErrorAsync(Strings.Format("Dialog_ImportFailed", ex.Message));
        }
    }

    private void PlaylistItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is YoutubePlaylist playlist)
        {
            ViewModel.AddYoutubePlaylistToQueue(playlist);
            this.Frame.Navigate(typeof(HomePage));
        }
    }

    private void PlayPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is YoutubePlaylist playlist)
        {
            ViewModel.AddYoutubePlaylistToQueue(playlist);
            this.Frame.Navigate(typeof(HomePage));
        }
    }

    private async void RemovePlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is YoutubePlaylist playlist)
        {
            await _playlistStorage.RemoveAsync(playlist.PlaylistId);
            for (int i = OnlinePlaylistItems.Count - 1; i >= 0; i--)
            {
                if (OnlinePlaylistItems[i].PlaylistId == playlist.PlaylistId)
                    OnlinePlaylistItems.RemoveAt(i);
            }
        }
    }

    private async void RefreshPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshingPlaylist || sender is not MenuFlyoutItem { Tag: YoutubePlaylist playlist })
            return;

        _isRefreshingPlaylist = true;
        var loading = DialogService.ShowLoading(Strings.Get("Dialog_RefreshingPlaylist"));

        try
        {
            var refreshedPlaylist = await YoutubeService.GetPlaylistAsync(playlist.PlaylistUrl);
            if (refreshedPlaylist is null)
            {
                await loading.HideAsync();
                await DialogService.ShowErrorAsync(Strings.Get("Dialog_FailedToRefreshPlaylist"));
                return;
            }

            await _playlistStorage.AddAsync(refreshedPlaylist);
            RefreshPlaylistItems();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Refresh playlist failed: {ex.Message}");
            await loading.HideAsync();
            await DialogService.ShowErrorAsync(Strings.Format("Dialog_RefreshFailed", ex.Message));
        }
        finally
        {
            await loading.HideAsync();
            _isRefreshingPlaylist = false;
        }

    }

    private void LibraryEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is LibraryEntry entry)
            _ = PlayLibraryEntryAsync(entry);
    }

    private void PlayEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: LibraryEntry entry })
            _ = PlayLibraryEntryAsync(entry);
    }

    private async Task PlayLibraryEntryAsync(LibraryEntry entry)
    {
        try
        {
            await Vm.PlayEntryAsync(entry);
            this.Frame.Navigate(typeof(HomePage));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to play library entry: {ex.Message}");
            await DialogService.ShowErrorAsync(Strings.Get("Dialog_LibraryPlayFailed"));
        }
    }

    private async void RemoveFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: LibraryEntry entry })
            await Vm.RemoveFavoriteAsync(entry);
    }

    private async void AddFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: LibraryEntry entry })
            await Vm.AddFavoriteAsync(entry);
    }

    private async void ClearRecent_Click(object sender, RoutedEventArgs e)
    {
        bool confirmed = await DialogService.ShowConfirmAsync(
            Strings.Get("Dialog_ClearRecentTitle"),
            Strings.Get("Dialog_ClearRecentConfirm"),
            Strings.Get("Dialog_ClearRecent"),
            Strings.Get("Dialog_Cancel"));
        if (!confirmed)
            return;

        await Vm.ClearRecentAsync();
    }

    public static Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InvertedBoolToVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Converts a stored thumbnail URL to an ImageSource, tolerating empty or
    /// invalid values (e.g. local files have no thumbnail URL). Returning null
    /// lets the placeholder icon behind the Image show through.
    /// </summary>
    public static ImageSource? EntryThumbnail(string? url) => MediaItem.CreateRemoteArt(url);
}