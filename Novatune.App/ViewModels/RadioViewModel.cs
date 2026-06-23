using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.ViewModels;

public partial class RadioViewModel : BaseViewModel
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };
    private static readonly Lock _cacheLock = new();
    private static List<string> _cachedServers = [];
    private static DateTime _cacheExpiry = DateTime.MinValue;

    static RadioViewModel()
    {
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Novatune/1.0");
    }

    private static async Task<List<string>> GetRadioBrowserServers(CancellationToken ct = default)
    {
        if (_cachedServers.Count > 0 && DateTime.UtcNow < _cacheExpiry)
            return _cachedServers;

        IPAddress[] ips;
        try
        {
            ips = await Dns.GetHostAddressesAsync("all.api.radio-browser.info", ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DNS failed: {ex.Message}");
            return [];
        }

        var hostNames = await Task.WhenAll(ips.Select(async ip =>
        {
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(ip.ToString(), ct);
                if (!string.IsNullOrEmpty(hostEntry.HostName))
                    return hostEntry.HostName;
            }
            catch (OperationCanceledException) { throw; }
            catch { }

            return (string?) null;
        }));

        var result = hostNames
           .Where(name => name is not null)
           .Select(name => name!)
           .Distinct()
           .OrderBy(_ => Random.Shared.Next())
           .ToList();

        if (result.Count > 0)
        {
            lock (_cacheLock)
            {
                if (_cachedServers.Count == 0 || DateTime.UtcNow >= _cacheExpiry)
                {
                    _cachedServers = result;
                    _cacheExpiry = DateTime.UtcNow.AddMinutes(15);
                }
            }
        }

        Debug.WriteLine($"Total servers found: {result.Count}");
        return result;
    }

    public static async Task<List<RadioItem>> SearchStationsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var servers = await GetRadioBrowserServers(cancellationToken);

        if (servers.Count == 0)
        {
            Debug.WriteLine("No servers found!");
            return [];
        }

        foreach (var server in servers)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10));

                var stations = await _http.GetFromJsonAsync<List<RadioItem>>(
                    $"https://{server}/json/stations/search?name={Uri.EscapeDataString(keyword)}&hidebroken=true&order=votes&reverse=true&limit=15&hls=0",
                    cts.Token
                );

                if (stations is not null && stations.Count > 0)
                {
                    Debug.WriteLine($"OK: {server}, found {stations.Count} stations");
                    return stations;
                }
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;

                Debug.WriteLine($"Timeout: {server}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed: {server} — {ex.Message}, trying next...");
            }
        }

        Debug.WriteLine("All servers failed!");
        return [];
    }
}