using System.Text.Json.Serialization;
using Windows.Media.Playback;

namespace Novatune.App.Models;

public partial class RadioItem
{
    [JsonPropertyName("stationuuid")]
    public string StationUuid { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    [JsonPropertyName("url_resolved")]
    public string UrlResolved { get; init; } = string.Empty;
    public string Favicon { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;

    public MediaPlaybackItem? PlaybackItem { get; set; }

    public override string ToString() => Name;
}
