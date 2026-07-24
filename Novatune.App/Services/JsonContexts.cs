using Novatune.App.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Novatune.App.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(List<RadioItem>))]
[JsonSerializable(typeof(RadioItem))]
internal partial class RadioJsonContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(IptvApiResponse))]
[JsonSerializable(typeof(List<IptvChannel>))]
internal partial class IptvJsonContext : JsonSerializerContext
{
}
