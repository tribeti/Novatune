using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.ViewModels;

public class RadioViewModel
{
    private static readonly HttpClient _http = new();
    private static List<string> _cachedServers = [];
    private static DateTime _cacheExpiry = DateTime.MinValue;

    private async static Task<List<string>> GetRadioBrowserServers(CancellationToken ct = default)
    {
        if (_cachedServers is not null && DateTime.Now < _cacheExpiry)
        {
            return _cachedServers;
        }

        IPAddress[] ips;
        try
        {
            ips = await Dns.GetHostAddressesAsync("all.api.radio-browser.info", ct);
        }
        catch
        {
            return [];
        }

        var servers = new List<(string HostName, long RoundTripTime)>();
        using var ping = new Ping();

        foreach (var ip in ips)
        {
            if (ct.IsCancellationRequested)
                break;
            try
            {
                var reply = await ping.SendPingAsync(ip, 2000);
                long rtt = (reply?.Status == IPStatus.Success) ? reply.RoundtripTime : long.MaxValue;
                var hostEntry = await Dns.GetHostEntryAsync(ip.ToString(), ct);
                string hostName = !string.IsNullOrEmpty(hostEntry.HostName) ? hostEntry.HostName : ip.ToString();

                servers.Add((hostName, rtt));
            }
            catch { }
        }

        var rng = new Random();
        var result = servers
            .OrderBy(s => s.RoundTripTime)
            .ThenBy(_ => rng.Next())
            .Select(s => s.HostName)
            .ToList();

        _cachedServers = result;
        _cacheExpiry = DateTime.Now.AddHours(1);

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