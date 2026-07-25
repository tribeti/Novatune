using Microsoft.Extensions.Caching.Memory;
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

namespace Novatune.App.Services;

public static class TVService
{
    private static readonly HttpClient _http = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        AutomaticDecompression = DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly MemoryCache _searchCache = new(new MemoryCacheOptions
    {
        SizeLimit = 100
    });


    static TVService()
    {
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Novatune/1.0 (mailto:contact@novatune.app)");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
    }

    public static async Task<List<IptvChannel>> SearchChannelsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var normalized = keyword.Trim().ToLowerInvariant();
        var cacheKey = $"iptv:{normalized}";

        if (_searchCache.TryGetValue(cacheKey, out List<IptvChannel>? cached) && cached is not null)
        {
            return CloneChannels(cached);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var url = $"https://tv-api-production-61f7.up.railway.app/api/v1/channels?q={Uri.EscapeDataString(normalized)}&has_stream=true&clean_streams=true&sort=streams_count&order=desc&limit=10";

            var response = await _http.GetFromJsonAsync(url, IptvJsonContext.Default.IptvApiResponse, cts.Token).ConfigureAwait(false);

            if (response?.Results is { Count: > 0 } results)
            {
                _searchCache.Set(cacheKey, results, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    Size = 1
                });

                return CloneChannels(results);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[IptvService] Search timed out");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IptvService] Search failed: {ex.Message}");
        }

        return [];
    }

    private static List<IptvChannel> CloneChannels(List<IptvChannel> source) =>
        source.Select(ch => new IptvChannel
        {
            Id = ch.Id,
            Name = ch.Name,
            Country = ch.Country,
            Logo = ch.Logo,
            Categories = [.. ch.Categories],
            StreamsCount = ch.StreamsCount,
            Streams = ch.Streams.Select(s => new IptvStream
            {
                Url = s.Url,
                Quality = s.Quality,
                Format = s.Format,
                IsWorking = s.IsWorking
            }).ToList()
        }).ToList();
}