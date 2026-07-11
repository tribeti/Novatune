using Microsoft.Extensions.Caching.Memory;
using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.Services;

public static class RadioService
{
    private static readonly HttpClient _http = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        AutomaticDecompression = DecompressionMethods.All
    })

    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static List<string> _cachedServers = [];
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _serverSemaphore = new(1, 1);

    private static readonly MemoryCache _searchCache = new(new MemoryCacheOptions
    {
        SizeLimit = 100
    });

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    static RadioService()
    {
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Novatune/1.0 (mailto:contact@novatune.app)");
    }

    private static async Task<List<string>> GetRadioBrowserServersAsync(CancellationToken ct = default)
    {
        if (_cachedServers.Count > 0 && DateTime.UtcNow < _cacheExpiry)
            return _cachedServers;

        await _serverSemaphore.WaitAsync(ct).ConfigureAwait(false);
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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Debug.WriteLine($"[RadioService] Get servers failed: {ex.Message}");
            }

            if (result.Count == 0)
            {
                result = ["de1.api.radio-browser.info", "de2.api.radio-browser.info", "at1.api.radio-browser.info"];
            }

            Shuffle(result);

            _cachedServers = result;
            _cacheExpiry = DateTime.UtcNow.Add(TimeSpan.FromMinutes(15));

            return result;
        }
        finally
        {
            _serverSemaphore.Release();
        }
    }

    public static async Task<List<RadioItem>> SearchStationsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var cacheKey = keyword.Trim().ToLowerInvariant();

        if (_searchCache.TryGetValue(cacheKey, out List<RadioItem>? cached) && cached is not null)
        {
            return [.. cached];
        }

        var servers = await GetRadioBrowserServersAsync(cancellationToken);

        if (servers.Count == 0)
            return [];

        foreach (var server in servers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(8));

                var url = $"https://{server}/json/stations/search?name={Uri.EscapeDataString(keyword)}&hidebroken=true&order=votes&reverse=true&limit=10";

                var stations = await _http.GetFromJsonAsync<List<RadioItem>>(url, _jsonOptions, cts.Token);

                if (stations is not null)
                {
                    _searchCache.Set(cacheKey, stations, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                        Size = 1
                    });

                    return stations;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Debug.WriteLine($"[RadioService] Server {server} failed: {ex.Message}");
            }
        }

        return [];
    }

    private static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}