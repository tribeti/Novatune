using DiscordRPC;
using DiscordRPC.Logging;
using System;

namespace Novatune.App.Services;

public sealed partial class DiscordRpcService : IDisposable
{
    private readonly DiscordRpcClient _client;
    private readonly SettingsService _settingsService;
    private bool _disposed;
    private const string ApplicationId = "1547684622664470649";

    public DiscordRpcService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _client = new DiscordRpcClient(ApplicationId)
        {
            Logger = new ConsoleLogger(LogLevel.Warning, true)
        };

        _client.OnReady += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Ready: {e.User.Username}");
        };

        _client.OnError += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Error: {e.Message}");
        };

        _client.OnConnectionFailed += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Connection failed: {e.Type}");
        };

        // Chỉ init khi user đã bật RPC trong settings.
        if (_settingsService.Settings.EnableDiscordRpc)
        {
            _client.Initialize();
        }
    }

    private bool EnsureInitialized()
    {
        if (_disposed)
            return false;

        if (!_settingsService.Settings.EnableDiscordRpc)
        {
            if (_client.IsInitialized)
            {
                _client.Deinitialize();
            }
            return false;
        }

        if (!_client.IsInitialized)
        {
            _client.Initialize();
        }

        return _client.IsInitialized;
    }

    /// <summary>
    /// Cập nhật Rich Presence (gọi khi đổi bài / pause / stop)
    /// </summary>
    public void UpdatePresence(
        string details,
        string state,
        string? largeImageKey = "logo",
        string? largeImageText = null,
        DateTime? startTime = null)
    {
        if (!EnsureInitialized())
            return;

        var presence = new RichPresence
        {
            Details = details,
            State = state,
            Type = ActivityType.Listening,
            Assets = new Assets
            {
                LargeImageKey = largeImageKey ?? "logo",
                LargeImageText = largeImageText ?? "Novatune"
            }
        };

        if (startTime.HasValue)
        {
            presence.Timestamps = new Timestamps(startTime.Value);
        }

        _client.SetPresence(presence);
    }

    /// <summary>
    /// Xóa presence (khi dừng phát hoặc tắt app)
    /// </summary>
    public void ClearPresence()
    {
        if (_disposed || !_client.IsInitialized)
            return;

        _client.ClearPresence();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_client.IsInitialized)
        {
            _client.ClearPresence();
        }

        _client.Dispose();
    }
}