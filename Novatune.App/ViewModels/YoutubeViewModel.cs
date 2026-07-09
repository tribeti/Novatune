using Novatune.App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Novatune.App.ViewModels;

public partial class YoutubeViewModel : BaseViewModel
{
    private static readonly YoutubeClient _youtube = new();

    public static async Task<List<YoutubeItem>> SearchVideosAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        var results = new List<YoutubeItem>();
        const int maxResults = 20;

        try
        {
            await foreach (var video in _youtube.Search.GetVideosAsync(keyword, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                results.Add(new YoutubeItem
                {
                    Title = video.Title,
                    Author = video.Author.ChannelTitle,
                    VideoUrl = video.Url,
                    ThumbnailUrl = video.Thumbnails.Count > 0 ? video.Thumbnails.GetWithHighestResolution().Url : string.Empty
                });

                if (results.Count >= maxResults)
                {
                    break;
                }
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed: {ex.Message}, trying next...");
            return [];
        }
    }
}