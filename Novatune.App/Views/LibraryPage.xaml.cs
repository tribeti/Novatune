using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Models;
using Novatune.App.Services;
using Novatune.App.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Novatune.App.Views;

public sealed partial class LibraryPage : Page
{
    public ObservableCollection<IptvChannel> TvChannelsItems { get; } = [];
    public ObservableCollection<YoutubePlaylist> OnlinePlaylistItems { get; } = [];

    private readonly MediaViewModel ViewModel = App.Current.Services.GetRequiredService<MediaViewModel>();
    private readonly PlaylistStorageService _playlistStorage = App.Current.Services.GetRequiredService<PlaylistStorageService>();

    public LibraryPage()
    {
        this.InitializeComponent();
        LoadTVList();
        LoadPlaylists();
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

    private void LoadTVList()
    {
        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV1",
            Country = "VN",
            Logo = "https://i.imgur.com/vZYlGIW.png",
            Categories = [],
            Streams =
            [
            new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv1/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv1/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv1/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV2",
            Country = "VN",
            Logo = "https://i.imgur.com/qWWpYRR.png",
            Categories = [],
            Streams =
            [
            new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv2/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv2/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv2/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV3",
            Country = "VN",
            Logo = "https://i.imgur.com/4lJG1fu.png",
            Categories = [],
            Streams =
            [
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv3/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv3/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
        ]
        });
    }

    private void TVItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is IptvChannel channel)
        {
            ViewModel.AddTV(channel);
            this.Frame.Navigate(typeof(HomePage));
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
            Title = "Import YouTube Playlist",
            Content = inputBox,
            PrimaryButtonText = "Import",
            CloseButtonText = "Cancel",
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
            Title = "Importing playlist...",
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
                    Title = "Error",
                    Content = "Failed to load playlist. Please check the URL and try again.",
                    CloseButtonText = "OK",
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
                Title = "Error",
                Content = $"Import failed: {ex.Message}",
                CloseButtonText = "OK",
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
}
