using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Novatune.App.Views;

public sealed partial class LibraryPage : Page
{
    public ObservableCollection<MediaItem11> RecentlyPlayedItems { get; } = [];
    public ObservableCollection<MediaItem11> TvChannelsItems { get; } = [];
    public ObservableCollection<MediaItem11> OnlinePlaylistItems { get; } = [];

    public LibraryPage()
    {
        this.InitializeComponent();
        LoadSampleData();
    }

    private void LoadSampleData()
    {
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Midnight City", ImageUrl = "https://picsum.photos/seed/music1/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Blinding Lights", ImageUrl = "https://picsum.photos/seed/music2/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Levitating", ImageUrl = "https://picsum.photos/seed/music3/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Peaches", ImageUrl = "https://picsum.photos/seed/music4/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Stay", ImageUrl = "https://picsum.photos/seed/music5/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Stay", ImageUrl = "https://picsum.photos/seed/music5/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Stay", ImageUrl = "https://picsum.photos/seed/music5/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Stay", ImageUrl = "https://picsum.photos/seed/music5/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Stay", ImageUrl = "https://picsum.photos/seed/music5/200/200" });
        RecentlyPlayedItems.Add(new MediaItem11 { Title = "Stay", ImageUrl = "https://picsum.photos/seed/music5/200/200" });

        TvChannelsItems.Add(new MediaItem11 { Title = "News 24/7", ImageUrl = "https://picsum.photos/seed/tv1/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Sports HD", ImageUrl = "https://picsum.photos/seed/tv2/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Movie Classics", ImageUrl = "https://picsum.photos/seed/tv3/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Movie Classics", ImageUrl = "https://picsum.photos/seed/tv3/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Movie Classics", ImageUrl = "https://picsum.photos/seed/tv3/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Movie Classics", ImageUrl = "https://picsum.photos/seed/tv3/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Movie Classics", ImageUrl = "https://picsum.photos/seed/tv3/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Movie Classics", ImageUrl = "https://picsum.photos/seed/tv3/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Movie Classics", ImageUrl = "https://picsum.photos/seed/tv3/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Movie Classics", ImageUrl = "https://picsum.photos/seed/tv3/200/200" });
        TvChannelsItems.Add(new MediaItem11 { Title = "Kids Zone", ImageUrl = "https://picsum.photos/seed/tv4/200/200" });

        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Chill Vibes Playlist" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Workout Mix 2024" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Top 50 Global" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
        OnlinePlaylistItems.Add(new MediaItem11 { Title = "Acoustic Covers" });
    }
}

public partial class MediaItem11
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}