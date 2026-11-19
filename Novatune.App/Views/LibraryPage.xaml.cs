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
        var inputBox = new TextBox
        {
            PlaceholderText = "https://youtube.com/playlist?list=...",
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
        };

        var dialog = new ContentDialog
        {
            Title = Strings.Get("Dialog_ImportPlaylistTitle"),
            Content = inputBox,
            PrimaryButtonText = Strings.Get("Dialog_Import"),
            CloseButtonText = Strings.Get("Dialog_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        var url = inputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
            return;

        // Show loading dialog
        var loadingDialog = new ContentDialog
        {
            Title = Strings.Get("Dialog_ImportingPlaylist"),
            Content = new ProgressRing { IsActive = true, Width = 48, Height = 48 },
            XamlRoot = this.XamlRoot,
        };

        _ = loadingDialog.ShowAsync();

        try
        {
            var playlist = await YoutubeService.GetPlaylistAsync(url);
            loadingDialog.Hide();

            if (playlist is null)
            {
                var errorDialog = new ContentDialog
                {
                    Title = Strings.Get("Dialog_Error"),
                    Content = Strings.Get("Dialog_FailedToLoadPlaylist"),
                    CloseButtonText = Strings.Get("Dialog_Ok"),
                    XamlRoot = this.XamlRoot,
                };
                await errorDialog.ShowAsync();
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
            loadingDialog.Hide();
            Debug.WriteLine($"Import playlist failed: {ex.Message}");

            var errorDialog = new ContentDialog
            {
                Title = Strings.Get("Dialog_Error"),
                Content = Strings.Format("Dialog_ImportFailed", ex.Message),
                CloseButtonText = Strings.Get("Dialog_Ok"),
                XamlRoot = this.XamlRoot,
            };
            await errorDialog.ShowAsync();
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
        var loadingDialog = new ContentDialog
        {
            Title = Strings.Get("Dialog_RefreshingPlaylist"),
            Content = new ProgressRing { IsActive = true },
            XamlRoot = this.XamlRoot,
        };

        var loadingTask = loadingDialog.ShowAsync();

        try
        {
            var refreshedPlaylist = await YoutubeService.GetPlaylistAsync(playlist.PlaylistUrl);
            if (refreshedPlaylist is null)
            {
                var errorDialog = new ContentDialog
                {
                    Title = Strings.Get("Dialog_Error"),
                    Content = Strings.Get("Dialog_FailedToRefreshPlaylist"),
                    CloseButtonText = Strings.Get("Dialog_Ok"),
                    XamlRoot = this.XamlRoot,
                };

                loadingDialog.Hide();
                await loadingTask;
                await errorDialog.ShowAsync();
                return;
            }

            await _playlistStorage.AddAsync(refreshedPlaylist);
            RefreshPlaylistItems();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Refresh playlist failed: {ex.Message}");

            var errorDialog = new ContentDialog
            {
                Title = Strings.Get("Dialog_Error"),
                Content = Strings.Format("Dialog_RefreshFailed", ex.Message),
                CloseButtonText = Strings.Get("Dialog_Ok"),
                XamlRoot = this.XamlRoot,
            };

            loadingDialog.Hide();
            await loadingTask;
            await errorDialog.ShowAsync();
        }
        finally
        {
            loadingDialog.Hide();
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

            var errorDialog = new ContentDialog
            {
                Title = Strings.Get("Dialog_Error"),
                Content = Strings.Get("Dialog_LibraryPlayFailed"),
                CloseButtonText = Strings.Get("Dialog_Ok"),
                XamlRoot = this.XamlRoot,
            };
            await errorDialog.ShowAsync();
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