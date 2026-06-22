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

public class RadioViewModel
{
    private static readonly HttpClient _http = new();
    private static List<string> _cachedServers = [];
    private static DateTime _cacheExpiry = DateTime.MinValue;

    private static async Task<List<string>> GetRadioBrowserServers(CancellationToken ct = default)
    {
        if (_cachedServers.Count > 0 && DateTime.Now < _cacheExpiry)
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

        var tasks = ips.Select(async ip =>
        {
            string? hostName = null;
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(ip.ToString(), ct);
                if (!string.IsNullOrEmpty(hostEntry.HostName))
                    hostName = hostEntry.HostName;
            }
            catch { }

            if (hostName is null)
            {
                Debug.WriteLine($"Skipping {ip}: reverse DNS failed");
                return (hostName: (string?) null, rtt: long.MaxValue);
            }

            Debug.WriteLine($"Server: {hostName}");
            return (hostName, rtt: 0L);
        }).ToList();

        var results = await Task.WhenAll(tasks);
        var rng = new Random();

        var result = results
            .Where(s => s.hostName is not null)
            .OrderBy(_ => rng.Next())
            .Select(s => s.hostName!)
            .Distinct()
            .ToList();

        if (result.Count > 0)
        {
            _cachedServers = result;
            _cacheExpiry = DateTime.Now.AddMinutes(15);
        }

        Debug.WriteLine($"Total servers found: {result.Count}");
        return result;
    }

    public static async Task<List<RadioItem>> SearchStationsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Novatune/1.0");
        var servers = await GetRadioBrowserServers(cancellationToken);

        if (servers.Count == 0)
        {
            Debug.WriteLine("No servers found!");
            return [];
        }

        foreach (var server in servers)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Debug.WriteLine($"Trying: {server}");
                var stations = await _http.GetFromJsonAsync<List<RadioItem>>(
                    $"https://{server}/json/stations/search?name={Uri.EscapeDataString(keyword)}&hidebroken=true&order=votes&reverse=true&limit=15&hls=0",
                    cancellationToken
                );

                if (stations is not null && stations.Count > 0)
                {
                    Debug.WriteLine($"OK: {server}, found {stations.Count} stations");
                    return stations;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Search cancelled");
                throw;
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