using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Novatune.App.Models;

public class IptvApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("results")]
    public List<IptvChannel> Results { get; set; } = [];
}

public class IptvChannel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("country")]
    public string Country { get; set; } = "";

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = [];

    [JsonPropertyName("streams_count")]
    public int StreamsCount { get; set; }

    [JsonPropertyName("streams")]
    public List<IptvStream> Streams { get; set; } = [];
}

public class IptvStream
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("is_working")]
    public bool IsWorking { get; set; }
}
