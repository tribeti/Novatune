using System.Collections.Generic;

namespace Novatune.App.Models;

public class YoutubePlaylist
{
    public string PlaylistId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string PlaylistUrl { get; set; } = string.Empty;
    public List<YoutubeItem> Videos { get; set; } = [];
}
