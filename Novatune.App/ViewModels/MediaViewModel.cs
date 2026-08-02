using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevWinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Novatune.App.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using WinRT.Interop;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Novatune.App.ViewModels;

public partial class MediaViewModel : BaseViewModel
{
    private static readonly Windows.Web.Http.HttpClient _winHttpClient = CreateHttpClient();
    private static Windows.Web.Http.HttpClient CreateHttpClient()
    {
        var client = new Windows.Web.Http.HttpClient();
        client.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Novatune/1.0");
        return client;
    }
    public MediaPlayer MediaPlayer { get; } = new();
    private readonly MediaPlaybackList _mediaPlaybackList = new();
    public ObservableCollection<MediaItem> Playlist { get; } = [];
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly DispatcherTimer _positionTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500)
    };
    private static readonly YoutubeClient _youtube = new();

    [ObservableProperty]
    public partial bool IsLive { get; set; } = false;

    public MediaViewModel()
    {
        MediaPlayer.PlaybackSession.PlaybackStateChanged += (s, _) =>
        {
            var playing = s.PlaybackState == MediaPlaybackState.Playing;

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (playing == IsPlaying)
                    return;

                IsPlaying = playing;
                if (IsPlaying)
                    _positionTimer.Start();
                else
                    _positionTimer.Stop();
            });
        };

        _positionTimer.Tick += (_, _) =>
        {
            var session = MediaPlayer.PlaybackSession;
            MediaDuration = session.NaturalDuration.TotalSeconds;
            PlaybackPosition = session.Position.TotalSeconds;

            if (!IsUserInteracting)
                TimelinePosition = PlaybackPosition;

            if (CurrentTrack is not null && !IsLive)
            {
                var duration = session.NaturalDuration;
                CurrentTrack.SavedPosition = duration > TimeSpan.Zero && duration - session.Position < TimeSpan.FromSeconds(20)
                    ? TimeSpan.Zero
                    : session.Position;
            }
        };

        _mediaPlaybackList.MaxPlayedItemsToKeepOpen = 3;
        _mediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
        _mediaPlaybackList.ItemFailed += MediaPlaybackList_ItemFailed;
        MediaPlayer.Source = _mediaPlaybackList;
        MediaPlayer.MediaFailed += (s, e) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine($"MediaPlayer failed: {e.Error}, HResult: 0x{e.ExtendedErrorCode?.HResult:X8}, Message: {e.ErrorMessage}");
            });
        };
    }

    #region Media buttons controls

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayGlyph))]
    public partial bool IsPlaying { get; set; } = false;
    public string PlayGlyph => IsPlaying ? "\uF8AE" : "\uF5B0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    public partial double MediaDuration { get; set; } = 0;
    public string DurationText
    {
        get
        {
            var ts = TimeSpan.FromSeconds(MediaDuration);
            return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionText))]
    public partial double PlaybackPosition { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionText))]
    public partial double TimelinePosition { get; set; } = 0;
    public string PositionText
    {
        get
        {
            var seconds = IsUserInteracting ? TimelinePosition : PlaybackPosition;
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        }
    }

    [ObservableProperty]
    public partial double Volume { get; set; } = 100.0;

    partial void OnVolumeChanged(double value)
    {
        MediaPlayer.Volume = Math.Clamp(value / 100.0, 0.0, 1.0);
    }

    [ObservableProperty]
    public partial bool IsUserInteracting { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QueueVisibility))]
    [NotifyPropertyChangedFor(nameof(QueueColumnWidth))]
    public partial bool IsQueueVisible { get; set; } = true;
    public Visibility QueueVisibility => IsQueueVisible ? Visibility.Visible : Visibility.Collapsed;
    public GridLength QueueColumnWidth => IsQueueVisible ? new GridLength(2, GridUnitType.Star) : new GridLength(0);

    public void ToggleQueue() => IsQueueVisible = !IsQueueVisible;

    public void PlayPause()
    {
        if (IsPlaying)
            MediaPlayer.Pause();
        else
            MediaPlayer.Play();
    }

    [RelayCommand]
    public void Seek(double seconds)
    {
        MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(seconds);
        PlaybackPosition = seconds;
    }

    public void CommitSeek()
    {
        MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(TimelinePosition);
        PlaybackPosition = TimelinePosition;
        IsUserInteracting = false;
    }

    public void Previous() => _mediaPlaybackList.MovePrevious();

    public void Next() => _mediaPlaybackList.MoveNext();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShuffleFontWeight))]
    public partial bool IsShuffleEnabled { get; set; } = false;
    public Windows.UI.Text.FontWeight ShuffleFontWeight =>
        IsShuffleEnabled ? Microsoft.UI.Text.FontWeights.ExtraBold : Microsoft.UI.Text.FontWeights.ExtraLight;

    public void Shuffle()
    {
        IsShuffleEnabled = !IsShuffleEnabled;
        _mediaPlaybackList.ShuffleEnabled = IsShuffleEnabled;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RepeatFontWeight))]
    public partial bool IsRepeatEnabled { get; set; } = false;
    public Windows.UI.Text.FontWeight RepeatFontWeight =>
        IsRepeatEnabled ? Microsoft.UI.Text.FontWeights.ExtraBold : Microsoft.UI.Text.FontWeights.ExtraLight;

    public void Repeat()
    {
        IsRepeatEnabled = !IsRepeatEnabled;
        _mediaPlaybackList.AutoRepeatEnabled = IsRepeatEnabled;
    }

    #endregion

    #region Media playlist controls

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BitmapImage? CurrentImage { get; set; }

    [ObservableProperty]
    public partial MediaItem? CurrentTrack { get; set; }

    [ObservableProperty]
    public partial bool IsMediaLoading { get; set; } = false;

    public readonly BitmapImage _defaultImage = new(new Uri("ms-appx:///Assets/LockScreenLogo.png"));

    [RelayCommand]
    public async Task AddLocalMedia()
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary, ViewMode = PickerViewMode.List };
        picker.FileTypeFilter.AddRange([".wmv", ".mp4", ".mkv", ".mp3", ".flac"]);
        var hwnd = WindowNative.GetWindowHandle((App.Current as App)!.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files is null || files.Count == 0)
            return;

        var tasks = files.Select(async file =>
        {
            StorageItemThumbnail? thumbnail = null;
            try
            {
                var source = MediaSource.CreateFromStorageFile(file);

                bool isVideo = file.FileType.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
                               file.FileType.Equals(".mkv", StringComparison.OrdinalIgnoreCase) ||
                               file.FileType.Equals(".wmv", StringComparison.OrdinalIgnoreCase);

                source.CustomProperties["Kind"] = SourceKind.Local.ToString();

                var musicProps = await file.Properties.GetMusicPropertiesAsync();
                thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 100);

                var playbackItem = new MediaPlaybackItem(source);

                var props = playbackItem.GetDisplayProperties();
                props.Type = isVideo ? MediaPlaybackType.Video : MediaPlaybackType.Music;

                string title = string.IsNullOrWhiteSpace(musicProps.Title) ? file.DisplayName : musicProps.Title;
                string artist = musicProps.Artist;

                if (isVideo)
                {
                    props.VideoProperties.Title = file.DisplayName;
                }
                else
                {
                    props.MusicProperties.Title = title;
                    props.MusicProperties.Artist = artist;
                }
                playbackItem.ApplyDisplayProperties(props);

                return new
                {
                    File = file,
                    Title = title,
                    Artist = artist,
                    Thumbnail = thumbnail,
                    PlaybackItem = playbackItem
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to process file {file.Name}: {ex.Message}");
                thumbnail?.Dispose();
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);

        try
        {
            var validResults = results.Where(r => r is not null);

            foreach (var res in validResults)
            {
                BitmapImage? bitmap = null;
                try
                {
                    if (res!.Thumbnail is not null)
                    {
                        bitmap = new BitmapImage
                        {
                            DecodePixelWidth = 100,
                            DecodePixelType = DecodePixelType.Physical
                        };
                        await bitmap.SetSourceAsync(res.Thumbnail);
                    }
                }
                finally
                {
                    res!.Thumbnail?.Dispose();
                }

                var track = MediaItem.FromLocal(res.File.Path, res.Title, res.Artist, bitmap, res.PlaybackItem);

                Playlist.Add(track);
                _mediaPlaybackList.Items.Add(track.PlaybackItem);
            }

            if (validResults.Any() && MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None)
                MediaPlayer.Play();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing media on UI thread: {ex.Message}");
        }
    }

    public void RemoveMedia(MediaItem? item)
    {
        if (item is null)
            return;

        var index = Playlist.IndexOf(item);
        if (index < 0)
            return;

        bool wasCurrent = _mediaPlaybackList.CurrentItem == item.PlaybackItem;
        bool wasPlaying = IsPlaying;

        if (wasCurrent && Playlist.Count > 1)
        {
            if (index == Playlist.Count - 1)
                _mediaPlaybackList.MovePrevious();
            else
                _mediaPlaybackList.MoveNext();
        }

        Playlist.RemoveAt(index);
        _mediaPlaybackList.Items.RemoveAt(index);

        if (item.PlaybackItem?.Source is IDisposable disposableSource)
        {
            disposableSource.Dispose();
        }

        if (Playlist.Count == 0)
        {
            MediaPlayer.Pause();
            MediaPlayer.Source = null;
            MediaPlayer.Source = _mediaPlaybackList;
        }

        else if (wasCurrent)
        {
            MediaPlayer.Pause();
            MediaPlayer.Source = null;
            MediaPlayer.Source = _mediaPlaybackList;

            if (wasPlaying)
            {
                MediaPlayer.Play();
            }
        }
    }

    public void AddRadio(RadioItem station) => AddRadio(station, playNow: true);

    public void AddRadioToQueue(RadioItem station) => AddRadio(station, playNow: false);

    private void AddRadio(RadioItem station, bool playNow)
    {
        var binder = new MediaBinder { Token = $"{station.UrlResolved}" };
        binder.Binding += Binder_Binding;

        var source = MediaSource.CreateFromMediaBinder(binder);
        source.CustomProperties["Kind"] = SourceKind.Radio.ToString();

        var playbackItem = new MediaPlaybackItem(source);

        var props = playbackItem.GetDisplayProperties();
        props.Type = MediaPlaybackType.Music;
        props.MusicProperties.Title = station.Name;
        props.MusicProperties.Artist = "Radio";
        playbackItem.ApplyDisplayProperties(props);

        var track = MediaItem.FromRadio(station, playbackItem);

        AddToPlaybackList(track, playNow);
    }

    public async void AddYoutube(YoutubeItem item) => await AddYoutube(item, playNow: true);

    public async void AddYoutubeToQueue(YoutubeItem item) => await AddYoutube(item, playNow: false);

    private async Task AddYoutube(YoutubeItem item, bool playNow)
    {
        _dispatcherQueue.TryEnqueue(() => IsMediaLoading = true);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var manifest = await _youtube.Videos.Streams.GetManifestAsync(item.VideoUrl, cts.Token);

            var streamInfo = manifest.GetMuxedStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestVideoQuality();

            if (streamInfo is not null)
            {
                var source = MediaSource.CreateFromUri(new Uri(streamInfo.Url));
                source.CustomProperties["Kind"] = SourceKind.Youtube.ToString();

                var playbackItem = new MediaPlaybackItem(source);

                var props = playbackItem.GetDisplayProperties();
                props.Type = MediaPlaybackType.Video;
                props.VideoProperties.Title = item.Title;
                props.VideoProperties.Subtitle = item.Author;
                playbackItem.ApplyDisplayProperties(props);

                var track = MediaItem.FromYoutube(item, playbackItem);

                AddToPlaybackList(track, playNow);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"[Youtube] Loading error: {e.Message}");
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsMediaLoading = false);
        }
    }

    public void AddYoutubePlaylistToQueue(YoutubePlaylist playlist)
    {
        if (playlist?.Videos is null || playlist.Videos.Count == 0)
            return;

        bool isFirst = true;
        foreach (var video in playlist.Videos)
        {
            if (Playlist.Any(t => t.SourcePathOrUrl == video.VideoUrl))
                continue;

            AddYoutubeBinding(video, playNow: isFirst);
            isFirst = false;
        }
    }

    private void AddYoutubeBinding(YoutubeItem item, bool playNow)
    {
        var binder = new MediaBinder { Token = item.VideoUrl };
        binder.Binding += Binder_Binding;

        var source = MediaSource.CreateFromMediaBinder(binder);
        source.CustomProperties["Kind"] = SourceKind.Youtube.ToString();

        var playbackItem = new MediaPlaybackItem(source);

        var props = playbackItem.GetDisplayProperties();
        props.Type = MediaPlaybackType.Video;
        props.VideoProperties.Title = item.Title;
        props.VideoProperties.Subtitle = item.Author;
        playbackItem.ApplyDisplayProperties(props);

        var track = MediaItem.FromYoutube(item, playbackItem);

        AddToPlaybackList(track, playNow);
    }

    public void AddTV(IptvChannel channel) => AddTV(channel, playNow: true);

    public void AddTVToQueue(IptvChannel channel) => AddTV(channel, playNow: false);

    private void AddTV(IptvChannel channel, bool playNow)
    {
        var stream = channel.Streams.FirstOrDefault(s => s.IsWorking) ?? channel.Streams.FirstOrDefault();
        if (stream is null || string.IsNullOrWhiteSpace(stream.Url))
            return;

        var streamUrl = stream.Url;

        var binder = new MediaBinder { Token = streamUrl };
        binder.Binding += Binder_Binding;

        var source = MediaSource.CreateFromMediaBinder(binder);
        source.CustomProperties["Kind"] = SourceKind.TV.ToString();

        var playbackItem = new MediaPlaybackItem(source);

        var props = playbackItem.GetDisplayProperties();
        props.Type = MediaPlaybackType.Video;
        props.VideoProperties.Title = channel.Name;
        props.VideoProperties.Subtitle = "TV";
        playbackItem.ApplyDisplayProperties(props);

        var track = MediaItem.FromTV(channel, streamUrl, playbackItem);

        AddToPlaybackList(track, playNow);
    }

    private void AddToPlaybackList(MediaItem track, bool playNow)
    {
        Playlist.Add(track);
        _mediaPlaybackList.Items.Add(track.PlaybackItem);

        if (playNow)
        {
            var index = _mediaPlaybackList.Items.Count - 1;
            _mediaPlaybackList.MoveTo((uint) index);
            MediaPlayer.Play();
        }
    }

    private async void Binder_Binding(MediaBinder sender, MediaBindingEventArgs args)
    {
        var deferral = args.GetDeferral();
        var url = sender.Token;

        try
        {
            var host = new Uri(url).Host.ToLowerInvariant();
            bool isYoutube = host.Contains("youtube.com") || host.Contains("youtu.be");
            bool isHls = url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase);

            if (isYoutube)
            {
                _dispatcherQueue.TryEnqueue(() => IsMediaLoading = true);
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var manifest = await _youtube.Videos.Streams.GetManifestAsync(url, cts.Token);
                    var streamInfo = manifest.GetMuxedStreams()
                        .Where(s => s.Container == Container.Mp4)
                        .GetWithHighestVideoQuality();

                    if (streamInfo is not null)
                    {
                        args.SetUri(new Uri(streamInfo.Url));
                    }
                }
                finally
                {
                    _dispatcherQueue.TryEnqueue(() => IsMediaLoading = false);
                }
            }
            else if (isHls)
            {
                var amsResult = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(url), _winHttpClient);
                if (amsResult.Status == AdaptiveMediaSourceCreationStatus.Success)
                {
                    args.SetAdaptiveMediaSource(amsResult.MediaSource);
                }
                else
                {
                    args.SetUri(new Uri(url));
                }
            }
            else
            {
                args.SetUri(new Uri(url));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Binding failed: {ex.Message}");
        }

        deferral.Complete();
    }

    public void PlayTrack(MediaItem track)
    {
        if (track?.PlaybackItem is null)
            return;

        if (_mediaPlaybackList.CurrentItem == track.PlaybackItem)
        {
            if (MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
            {
                MediaPlayer.Play();
            }
            return;
        }

        var index = _mediaPlaybackList.Items.IndexOf(track.PlaybackItem);

        if (index >= 0)
        {
            _mediaPlaybackList.MoveTo((uint) index);
            MediaPlayer.Play();
        }
    }

    private void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            CurrentTrack?.IsCurrent = false;

            if (args.NewItem is null)
            {
                Title = string.Empty;
                CurrentImage = _defaultImage;
                CurrentTrack = null;
                IsLive = false;
                return;
            }

            var kind = args.NewItem.Source.CustomProperties["Kind"] as string ?? "Unknown";
            IsLive = (kind == SourceKind.Radio.ToString() || kind == SourceKind.TV.ToString());

            var currentTrack = Playlist.FirstOrDefault(t => t.PlaybackItem == args.NewItem);
            if (currentTrack is not null)
            {
                currentTrack.IsCurrent = true;
                Title = currentTrack.Title;
                CurrentImage = currentTrack.Thumbnail ?? _defaultImage;
                CurrentTrack = currentTrack;

                if (!IsLive && currentTrack.SavedPosition > TimeSpan.Zero)
                    MediaPlayer.PlaybackSession.Position = currentTrack.SavedPosition;
            }
            else
            {
                CurrentTrack = null;
            }
        });
    }

    private void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
    {
        Debug.WriteLine($"ItemFailed: {args.Error.ErrorCode}");
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                var failedTrack = Playlist.FirstOrDefault(t => t.PlaybackItem == args.Item);
                Debug.WriteLine($"ItemFailed: errorCode={args?.Error?.ErrorCode}");

                if (failedTrack is not null)
                    RemoveMedia(failedTrack);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Debug.WriteLine($"ItemFailed cleanup COMException: HResult=0x{ex.HResult:X8}, Message={ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ItemFailed cleanup unexpected exception: {ex}");
            }
        });
    }

    #endregion
}
