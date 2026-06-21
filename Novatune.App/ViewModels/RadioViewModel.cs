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

    private static List<string> GetRadioBrowserServers()
    {
        var ips = Dns.GetHostAddresses("all.api.radio-browser.info");
        var servers = new List<(string HostName, long RoundTripTime)>();

        foreach (IPAddress ip in ips)
        {
            try
            {
                var reply = new Ping().Send(ip);
                long rtt = (reply is not null && reply.Status == IPStatus.Success)
                    ? reply.RoundtripTime
                    : long.MaxValue;

                var hostEntry = Dns.GetHostEntry(ip);
                string hostName = !string.IsNullOrEmpty(hostEntry.HostName)
                    ? hostEntry.HostName
                    : ip.ToString();

                servers.Add((hostName, rtt));
                Debug.WriteLine($"Server: {hostName}, RTT: {rtt}ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Skipped {ip}: {ex.Message}");
            }
        }

        var rng = new Random();
        return servers
            .OrderBy(s => s.RoundTripTime)
            .ThenBy(_ => rng.Next())
            .Select(s => s.HostName)
            .ToList();
    }

    public static async Task<List<RadioItem>> SearchStationsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Novatune/1.0");
        var servers = GetRadioBrowserServers();

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