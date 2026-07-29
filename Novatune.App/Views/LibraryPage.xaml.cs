using Microsoft.UI.Xaml.Controls;
using Novatune.App.Models;
using System.Collections.ObjectModel;

namespace Novatune.App.Views;

public sealed partial class LibraryPage : Page
{
    public ObservableCollection<IptvChannel> TvChannelsItems { get; } = [];
    public ObservableCollection<YoutubeItem> OnlinePlaylistItems { get; } = [];

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
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv1-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
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
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv2-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
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
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv3-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv3/live247-hls-avc/index.m3u8", Quality = "", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV4",
            Country = "VN",
            Logo = "https://i.imgur.com/WsLLmR2.png",
            Categories = [],
            Streams =
            [
                new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv4/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv4/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv4/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv4-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV5",
            Country = "VN",
            Logo = "https://i.imgur.com/Vnnna1C.png",
            Categories = [],
            Streams =
            [
                new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv5/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv5/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv5-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv5/live247-hls-avc/index.m3u8", Quality = "", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV5 Tay Nam Bo",
            Country = "VN",
            Logo = "https://i.imgur.com/xiuHjEQ.png",
            Categories = [],
            Streams =
            [
                new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv5tnb/live-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv5tnb-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV5 Tay Nguyen",
            Country = "VN",
            Logo = "https://i.imgur.com/Hlcnqqt.png",
            Categories = [],
            Streams =
            [
                new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv5tn/live-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv5tn-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV6",
            Country = "VN",
            Logo = "https://upload.wikimedia.org/wikipedia/commons/thumb/b/b0/VTV6_logo_2026_final.svg/960px-VTV6_logo_2026_final.svg.png",
            Categories = ["sports"],
            Streams =
            [
                new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv6/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv6/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv6/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv6tt-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV7",
            Country = "VN",
            Logo = "https://i.imgur.com/ZwoP823.png",
            Categories = [],
            Streams =
            [
                new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv7/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv7/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv7/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv7-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV8",
            Country = "VN",
            Logo = "https://i.imgur.com/rf9adGJ.png",
            Categories = [],
            Streams =
            [
                new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv8/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv8/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv8/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv8-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV9",
            Country = "VN",
            Logo = "https://i.imgur.com/QiY55sy.png",
            Categories = [],
            Streams =
            [
                new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv9/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv9/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv9/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vtvgolive-failover.vtvdigital.vn/vtvgo/vtv9-manifest.m3u8", Quality = "720p", Format = "hls", IsWorking = true },
        ]
        });

        TvChannelsItems.Add(new IptvChannel
        {
            Name = "VTV10",
            Country = "VN",
            Logo = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/70/VTV10_logo_2026.png/960px-VTV10_logo_2026.png",
            Categories = [],
            Streams =
            [
            new IptvStream { Url = "https://live-a.fptplay53.net/live/media/vtv10/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://live.fptplay53.net/live/media/vtv10/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
            new IptvStream { Url = "https://vips-livecdn.fptplay.net/live/media/vtv10/live247-hls-avc/index.m3u8", Quality = "1080p", Format = "hls", IsWorking = true },
        ]
        });

        OnlinePlaylistItems.Add(new YoutubeItem { Title = "Chill Vibes Playlist", Author = "ChillNation", VideoUrl = "https://youtube.com/watch?v=example1", ThumbnailUrl = "https://img.icons8.com/color/150/youtube-play.png" });
        OnlinePlaylistItems.Add(new YoutubeItem { Title = "Workout Power Mix", Author = "GymBeats", VideoUrl = "https://youtube.com/watch?v=example2", ThumbnailUrl = "https://img.icons8.com/color/150/youtube-play.png" });
        OnlinePlaylistItems.Add(new YoutubeItem { Title = "Lo-fi Study Beats", Author = "StudyMusic", VideoUrl = "https://youtube.com/watch?v=example3", ThumbnailUrl = "https://img.icons8.com/color/150/youtube-play.png" });
        OnlinePlaylistItems.Add(new YoutubeItem { Title = "Top Hits 2026", Author = "MusicTrends", VideoUrl = "https://youtube.com/watch?v=example4", ThumbnailUrl = "https://img.icons8.com/color/150/youtube-play.png" });
        OnlinePlaylistItems.Add(new YoutubeItem { Title = "Acoustic Covers", Author = "AcousticVibes", VideoUrl = "https://youtube.com/watch?v=example5", ThumbnailUrl = "https://img.icons8.com/color/150/youtube-play.png" });
        OnlinePlaylistItems.Add(new YoutubeItem { Title = "V-Pop Ballad Collection", Author = "VietMusic", VideoUrl = "https://youtube.com/watch?v=example6", ThumbnailUrl = "https://img.icons8.com/color/150/youtube-play.png" });
    }
}
