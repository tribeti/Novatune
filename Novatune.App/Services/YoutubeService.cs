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
                string thumbnailUrl = GetPreferredThumbnailUrl(video.Thumbnails);

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

    public static async Task<YoutubePlaylist?> GetPlaylistAsync(string playlistUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playlistUrl))
        {
            return null;
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var token = linkedCts.Token;

        try
        {
            var playlistMeta = await _youtube.Playlists.GetAsync(playlistUrl, token).ConfigureAwait(false);

            var playlist = new YoutubePlaylist
            {
                PlaylistId = playlistMeta.Id.Value,
                Title = playlistMeta.Title,
                Author = playlistMeta.Author?.ChannelTitle ?? string.Empty,
                ThumbnailUrl = playlistMeta.Thumbnails
                    .OrderByDescending(t => t.Resolution.Height)
                    .FirstOrDefault()?.Url ?? string.Empty,
                PlaylistUrl = playlistUrl.Trim(),
                Videos = [],
            };

            await foreach (var batch in _youtube.Playlists.GetVideoBatchesAsync(playlistUrl, token).ConfigureAwait(false))
            {
                foreach (var video in batch.Items)
                {
                    string thumbnailUrl = GetPreferredThumbnailUrl(video.Thumbnails);

                    playlist.Videos.Add(new YoutubeItem
                    {
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        VideoUrl = video.Url,
                        ThumbnailUrl = thumbnailUrl
                    });
                }
            }

            return playlist;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"YouTube playlist fetch failed: {ex.Message}");
            return null;
        }
    }

    private static string GetPreferredThumbnailUrl(IReadOnlyList<Thumbnail> thumbnails)
    {
        return thumbnails
            .FirstOrDefault(t => t.Resolution.Height is >= 360 and <= 480)?.Url
            ?? thumbnails.GetWithHighestResolution().Url
            ?? string.Empty;
    }
}