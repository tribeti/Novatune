using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Media.Playback;

namespace Novatune.App.Models;

public enum SourceKind
{
    Local,
    Radio,
    Youtube,
    HLS,
    TV
}

public partial class MediaItem : ObservableObject
{
    public SourceKind Kind { get; init; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Subtitle { get; set; } = string.Empty;
    [ObservableProperty]
    public partial BitmapImage? Thumbnail { get; set; }
    [ObservableProperty]
    public partial bool IsCurrent { get; set; }
    public MediaPlaybackItem PlaybackItem { get; set; } = null!;
    public string? SourcePathOrUrl { get; set; }
    public object? SourceItem { get; set; }
    public TimeSpan SavedPosition { get; set; } = TimeSpan.Zero;

    public static MediaItem FromLocal(string path, string title, string artist, BitmapImage? img, MediaPlaybackItem item)
    {
        return new MediaItem
        {
            Kind = SourceKind.Local,
            Title = title,
            Subtitle = string.IsNullOrWhiteSpace(artist) ? "Local File" : artist,
            Thumbnail = img,
            PlaybackItem = item,
            SourcePathOrUrl = path
        };
    }

    public static MediaItem FromRadio(RadioItem radio, MediaPlaybackItem item)
    {
        BitmapImage? img = null;
        if (!string.IsNullOrWhiteSpace(radio.Favicon))
        {
            var url = radio.Favicon.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "https://" + radio.Favicon[7..] : radio.Favicon;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                img = new BitmapImage(uri);
        }

        return new MediaItem
        {
            Kind = SourceKind.Radio,
            Title = radio.Name,
            Subtitle = string.IsNullOrWhiteSpace(radio.Tags) ? "Radio Station" : radio.Tags,
            Thumbnail = img ?? new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.png")),
            PlaybackItem = item,
            SourcePathOrUrl = radio.UrlResolved
        };
    }

    public static MediaItem FromYoutube(YoutubeItem youtube, MediaPlaybackItem item)
    {
        BitmapImage? img = null;
        if (!string.IsNullOrWhiteSpace(youtube.ThumbnailUrl))
        {
            if (Uri.TryCreate(youtube.ThumbnailUrl, UriKind.Absolute, out var uri))
                img = new BitmapImage(uri);
        }

        return new MediaItem
        {
            Kind = SourceKind.Youtube,
            Title = youtube.Title,
            Subtitle = youtube.Author,
            Thumbnail = img ?? new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.png")),
            PlaybackItem = item,
            SourcePathOrUrl = youtube.VideoUrl
        };
    }

    public static MediaItem FromTV(IptvChannel channel, string streamUrl, MediaPlaybackItem item)
    {
        BitmapImage? img = null;
        if (!string.IsNullOrWhiteSpace(channel.Logo))
        {
            var url = channel.Logo.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "https://" + channel.Logo[7..] : channel.Logo;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                img = new BitmapImage(uri);
        }

        var subtitle = channel.Categories.Count > 0
            ? string.Join(", ", channel.Categories)
            : channel.Country;

        return new MediaItem
        {
            Kind = SourceKind.TV,
            Title = channel.Name,
            Subtitle = string.IsNullOrWhiteSpace(subtitle) ? "TV Channel" : subtitle,
            Thumbnail = img ?? new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.png")),
            PlaybackItem = item,
            SourcePathOrUrl = streamUrl
        };
    }
}