using System;

namespace Novatune.App.Models;

public class YoutubeItem
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string VideoUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public TimeSpan Duration;
}
