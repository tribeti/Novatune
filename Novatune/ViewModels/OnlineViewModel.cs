using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using Novatune.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;

namespace Novatune.ViewModels
{
    public partial class OnlineViewModel : ObservableObject
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly MediaPlayerViewModel _mediaPlayerViewModel;

        [ObservableProperty]
        private string _searchQuery;

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<OnlineModel> Videos { get; } = new ();

        public OnlineViewModel(MediaPlayerViewModel mediaPlayerVM)
        {
            var httpClient = new HttpClient ();
            httpClient.DefaultRequestHeaders.Add ("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _youtubeClient = new YoutubeClient(httpClient);
            _mediaPlayerViewModel = mediaPlayerVM ?? throw new ArgumentNullException(nameof(mediaPlayerVM));
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
                return;

            IsLoading = true;
            Videos.Clear();

            try
            {
                var searchResults = _youtubeClient.Search.GetVideosAsync(SearchQuery);
                await foreach (var video in searchResults)
                {
                    var onlineModel = new OnlineModel
                    {
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        DurationTimeSpan = video.Duration,
                        StreamUrl = "",
                        ThumbnailUrl = video.Thumbnails.GetWithHighestResolution()?.Url ?? "",
                        VideoId = video.Id.Value
                    };

                    Videos.Add(onlineModel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tìm kiếm YouTube: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task PlayVideoAsync(OnlineModel selectedVideo)
        {
            if (selectedVideo != null && _mediaPlayerViewModel != null)
            {
                try
                {
                    if (string.IsNullOrEmpty(selectedVideo.StreamUrl))
                    {
                        selectedVideo.StreamUrl = await GetStreamUrlAsync(selectedVideo.VideoId);
                    }

                    if (!string.IsNullOrEmpty(selectedVideo.StreamUrl))
                    {
                        if (_mediaPlayerViewModel.PlayOnlineAudioCommand.CanExecute(selectedVideo))
                        {
                            await _mediaPlayerViewModel.PlayOnlineAudioCommand.ExecuteAsync(selectedVideo);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Không thể lấy stream URL cho: {selectedVideo.Title}");
                    }
                }
                catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi 403 (Forbidden) khi lấy stream URL cho {selectedVideo.Title}: {httpEx.Message}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi lấy stream URL hoặc phát video {selectedVideo.Title}: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
        private async Task<string> GetStreamUrlAsync(string videoId)
        {
            try
            {
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
                var audioStreamInfo = streamManifest
                    .GetAudioOnlyStreams()
                    .TryGetWithHighestBitrate();

                return audioStreamInfo?.Url ?? "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lấy stream URL: {ex.Message}");
                return "";
            }
        }
    }
}
