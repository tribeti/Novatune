using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Models;
using Novatune.App.ViewModels;
using System.Collections.ObjectModel;

namespace Novatune.App.Views;

public sealed partial class LibraryPage : Page
{
    public ObservableCollection<IptvChannel> TvChannelsItems { get; } = [];
    public ObservableCollection<YoutubeItem> OnlinePlaylistItems { get; } = [];

    private readonly MediaViewModel ViewModel = App.Current.Services.GetRequiredService<MediaViewModel>();

    public LibraryPage()
    {
        this.InitializeComponent();
        LoadSampleData();
    }

    private void LoadSampleData()
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

    private void TVItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is IptvChannel channel)
        {
            ViewModel.AddTV(channel);
        }
    }
}
