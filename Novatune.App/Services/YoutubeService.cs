using Microsoft.Extensions.Caching.Memory;
using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Novatune.App.Services;

public static class YoutubeService
{
    private const int MaxResults = 20;

    private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        AutomaticDecompression = DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly YoutubeClient _youtube = new(_httpClient);

    private static readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 200
    });

    public static async Task<List<YoutubeItem>> SearchVideosAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        var cacheKey = keyword.Trim().ToLowerInvariant();

        if (_cache.TryGetValue(cacheKey, out List<YoutubeItem>? cached) && cached is not null)
        {
            return [.. cached];
        }

        var results = new List<YoutubeItem>(MaxResults);

        try
        {
            await foreach (var video in _youtube.Search.GetVideosAsync(keyword, cancellationToken).ConfigureAwait(false))
            {
                string thumbnailUrl = video.Thumbnails
                    .FirstOrDefault(t => t.Resolution.Height is >= 360 and <= 480)?.Url
                    ?? video.Thumbnails.GetWithHighestResolution().Url;

                results.Add(new YoutubeItem
                {
                    Title = video.Title,
                    Author = video.Author.ChannelTitle,
                    VideoUrl = video.Url,
                    ThumbnailUrl = thumbnailUrl
                });

                if (results.Count >= MaxResults)
                {
                    break;
                }
            }

            _cache.Set(cacheKey, results, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Size = 1
            });

            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"YouTube search failed: {ex.Message}");
            return [];
        }
    }
}