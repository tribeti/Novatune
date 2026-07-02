using System.Text.Json.Serialization;

namespace Novatune.App.Models;

public class RadioItem
{
    [JsonPropertyName("stationuuid")]
    public string StationUuid { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("url_resolved")]
    public string UrlResolved { get; init; } = string.Empty;

    [JsonPropertyName("favicon")]
    public string Favicon { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public string Tags { get; init; } = string.Empty;

    public override string ToString() => Name;
}