using System;
using System.IO;
using System.Text.Json;

namespace Novatune.App.Services;

public enum AppTheme
{
    Default = 0,
    Light = 1,
    Dark = 2
}

public class AppSettings
{
    public const string DefaultLanguage = "en-US";

    public bool MinimizeOnClose { get; set; } = false;
    public bool EnableDiscordRpc { get; set; } = false;
    public AppTheme Theme { get; set; } = AppTheme.Default;
    public string Language { get; set; } = DefaultLanguage;
}

public class SettingsService
{
    private readonly string _settingsPath;
    public AppSettings Settings { get; private set; }

    public SettingsService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(localAppData, "Novatune");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");

        Settings = Load();
    }

    private AppSettings Load()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                string json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
        return new AppSettings();
    }

    public bool Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(Settings, SettingsJsonContext.Default.AppSettings);
            File.WriteAllText(_settingsPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
