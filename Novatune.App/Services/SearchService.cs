using Novatune.App.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Novatune.App.Services;

public static class SearchService
{
    public static async Task<List<MediaItem>> SearchAllAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var stationsTask = RadioService.SearchStationsAsync(keyword, cancellationToken);
        var videosTask = YoutubeService.SearchVideosAsync(keyword, cancellationToken);
        var tvTask = TVService.SearchChannelsAsync(keyword, cancellationToken);

        await Task.WhenAll(stationsTask, videosTask, tvTask).ConfigureAwait(false);

        var results = new List<MediaItem>(stationsTask.Result.Count + videosTask.Result.Count + tvTask.Result.Count);

        results.AddRange(stationsTask.Result.Select(MapRadio));
        results.AddRange(videosTask.Result.Select(MapYoutube));

        results.AddRange(tvTask.Result
            .Where(ch => ch.Streams.Count > 0)
            .Select(MapTv));

        return results;
    }

    private static MediaItem MapRadio(RadioItem station) => new()
    {
        Kind = SourceKind.Radio,
        Title = station.Name,
        Subtitle = string.IsNullOrWhiteSpace(station.Tags) ? "Radio Station" : station.Tags,
        SourceItem = station
    };

    private static MediaItem MapYoutube(YoutubeItem video) => new()
    {
        Kind = SourceKind.Youtube,
        Title = video.Title,
        Subtitle = video.Author,
        SourceItem = video
    };

    private static MediaItem MapTv(IptvChannel channel)
    {
        var subtitle = channel.Categories.Count > 0
            ? string.Join(", ", channel.Categories)
            : channel.Country;

        return new MediaItem
        {
            Kind = SourceKind.TV,
            Title = channel.Name,
            Subtitle = string.IsNullOrWhiteSpace(subtitle) ? "TV Channel" : subtitle,
            SourceItem = channel
        };
    }
}
