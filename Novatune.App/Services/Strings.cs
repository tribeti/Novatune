using Microsoft.Windows.ApplicationModel.Resources;

namespace Novatune.App.Services;

/// <summary>
/// Central access to localized string resources (Strings/&lt;lang&gt;/Resources.resw).
/// </summary>
public static class Strings
{
    private static ResourceLoader _loader = new();

    public static string Get(string key) => _loader.GetString(key);

    public static string Format(string key, params object?[] args) => string.Format(_loader.GetString(key), args);

    /// <summary>
    /// Rebuilds the loader so subsequent lookups pick up a PrimaryLanguageOverride change.
    /// </summary>
    public static void Reset() => _loader = new ResourceLoader();
}
