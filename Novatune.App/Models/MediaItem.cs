using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Novatune.App.Services;
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
    private static readonly BitmapImage FallbackArt = new(new Uri("ms-appx:///Assets/LockScreenLogo.png"));

    private static BitmapImage? CreateRemoteArt(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var absolute = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? "https://" + url[7..]
            : url;

        if (!Uri.TryCreate(absolute, UriKind.Absolute, out var uri))
            return null;

        return new BitmapImage
        {
            DecodePixelWidth = 256,
            DecodePixelType = DecodePixelType.Physical,
            UriSource = uri
        };
    }

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
            Subtitle = string.IsNullOrWhiteSpace(artist) ? Strings.Get("Media_LocalFile") : artist,
            Thumbnail = img,
            PlaybackItem = item,
            SourcePathOrUrl = path
        };
    }

    public static MediaItem FromRadio(RadioItem radio, MediaPlaybackItem item)
    {
        return new MediaItem
        {
            Kind = SourceKind.Radio,
            Title = radio.Name,
            Subtitle = string.IsNullOrWhiteSpace(radio.Tags) ? Strings.Get("Media_RadioStation") : radio.Tags,
            Thumbnail = CreateRemoteArt(radio.Favicon) ?? FallbackArt,
            PlaybackItem = item,
            SourcePathOrUrl = radio.UrlResolved
        };
    }

    public static MediaItem FromYoutube(YoutubeItem youtube, MediaPlaybackItem item)
    {
        return new MediaItem
        {
            Kind = SourceKind.Youtube,
            Title = youtube.Title,
            Subtitle = youtube.Author,
            Thumbnail = CreateRemoteArt(youtube.ThumbnailUrl) ?? FallbackArt,
            PlaybackItem = item,
            SourcePathOrUrl = youtube.VideoUrl
        };
    }

    public static MediaItem FromTV(IptvChannel channel, string streamUrl, MediaPlaybackItem item)
    {
        var subtitle = channel.Categories.Count > 0
            ? string.Join(", ", channel.Categories)
            : channel.Country;

        return new MediaItem
        {
            Kind = SourceKind.TV,
            Title = channel.Name,
            Subtitle = string.IsNullOrWhiteSpace(subtitle) ? Strings.Get("Media_TVChannel") : subtitle,
            Thumbnail = CreateRemoteArt(channel.Logo) ?? FallbackArt,
            PlaybackItem = item,
            SourcePathOrUrl = streamUrl
        };
    }
}