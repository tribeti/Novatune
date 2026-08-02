using System.Text.Json.Serialization;

namespace Novatune.App.Models;

public class RadioItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url_resolved")]
    public string UrlResolved { get; set; } = string.Empty;

    [JsonPropertyName("favicon")]
    public string Favicon { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    public override string ToString() => Name;
}