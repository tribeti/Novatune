using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.Services;

public class RadioService
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };
    private static readonly SemaphoreSlim _cacheSemaphore = new(1, 1);
    private static List<string> _cachedServers = [];
    private static DateTime _cacheExpiry = DateTime.MinValue;

    static RadioService()
    {
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Novatune/1.0");
    }

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static async Task<List<string>> GetRadioBrowserServers(CancellationToken ct = default)
    {
        if (_cachedServers.Count > 0 && DateTime.UtcNow < _cacheExpiry)
            return _cachedServers;

        await _cacheSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cachedServers.Count > 0 && DateTime.UtcNow < _cacheExpiry)
                return _cachedServers;

            var result = new List<string>();

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10));

                var json = await _http.GetStringAsync("https://de1.api.radio-browser.info/json/servers", cts.Token).ConfigureAwait(false);
                var servers = JsonSerializer.Deserialize<List<RadioItem>>(json, _jsonOptions);

                result = servers?
                    .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                    .Select(s => s.Name!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? [];
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"API failed: {ex.Message}");
            }

            if (result.Count == 0)
            {
                result = ["de1.api.radio-browser.info", "de2.api.radio-browser.info", "at1.api.radio-browser.info"];
            }

            Shuffle(result, Random.Shared);

            _cachedServers = result;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(15);

            Debug.WriteLine($"Total servers found: {result.Count}");
            return result;
        }
        finally
        {
            _cacheSemaphore.Release();
        }
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
                    $"https://{server}/json/stations/search?name={Uri.EscapeDataString(keyword)}&hidebroken=true&order=votes&reverse=true&limit=30",
                    _jsonOptions,
                    cts.Token
                );

                if (stations is not null)
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

    private static void Shuffle<T>(List<T> list, Random random)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}
