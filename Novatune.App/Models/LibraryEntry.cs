using System;

namespace Novatune.App.Models;

/// <summary>
/// A persisted library record (favourite or play-history entry) describing any
/// playable media source (local file, YouTube, radio, TV) so it can be replayed later.
/// </summary>
public class LibraryEntry
{
    public SourceKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    /// <summary>Local file path or stream URL identifying the media.</summary>
    public string SourcePathOrUrl { get; set; } = string.Empty;
    /// <summary>Remote image URL; empty for local files (thumbnail is regenerated on demand).</summary>
    public string ThumbnailUrl { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Stable identity used for de-duplication across kinds.</summary>
    public string Key => $"{(int)Kind}|{SourcePathOrUrl}";

    /// <summary>
    /// Maps a currently playing track to a persisted library record,
    /// extracting the remote thumbnail URL when the source item carries one.
    /// </summary>
    public static LibraryEntry FromTrack(MediaItem track)
    {
        var thumbnailUrl = track.Kind switch
        {
            SourceKind.Youtube => (track.SourceItem as YoutubeItem)?.ThumbnailUrl ?? string.Empty,
            SourceKind.Radio => (track.SourceItem as RadioItem)?.Favicon ?? string.Empty,
            SourceKind.TV => (track.SourceItem as IptvChannel)?.Logo ?? string.Empty,
            _ => string.Empty
        };

        return new LibraryEntry
        {
            Kind = track.Kind,
            Title = track.Title,
            Subtitle = track.Subtitle,
            SourcePathOrUrl = track.SourcePathOrUrl ?? string.Empty,
            ThumbnailUrl = thumbnailUrl
        };
    }
}
