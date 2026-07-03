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
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Novatune.App.ViewModels;

public partial class MediaViewModel : BaseViewModel
{
    public MediaPlayer MediaPlayer { get; } = new();
    private readonly MediaPlaybackList _mediaPlaybackList = new();
    public ObservableCollection<MediaItem> Playlist { get; } = [];
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly DispatcherTimer _positionTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500)
    };

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
    public partial bool IsQueueVisible { get; set; } = true;
    public Visibility QueueVisibility => IsQueueVisible ? Visibility.Visible : Visibility.Collapsed;

    [RelayCommand]
    public void ToggleQueue() => IsQueueVisible = !IsQueueVisible;

    [RelayCommand]
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

    [RelayCommand]
    public void CommitSeek()
    {
        MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(TimelinePosition);
        PlaybackPosition = TimelinePosition;
        IsUserInteracting = false;
    }

    [RelayCommand]
    public void Previous() => _mediaPlaybackList.MovePrevious();

    [RelayCommand]
    public void Next() => _mediaPlaybackList.MoveNext();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShuffleFontWeight))]
    public partial bool IsShuffleEnabled { get; set; } = false;
    public Windows.UI.Text.FontWeight ShuffleFontWeight =>
        IsShuffleEnabled ? Microsoft.UI.Text.FontWeights.ExtraBold : Microsoft.UI.Text.FontWeights.ExtraLight;

    [RelayCommand]
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

    [RelayCommand]
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
    public partial int PlaylistIndex { get; set; } = -1;

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
            var source = MediaSource.CreateFromStorageFile(file);
            bool isVideo = file.FileType.ToLower() is ".mp4" or ".mkv" or ".wmv";
            source.CustomProperties["Kind"] = SourceKind.Local.ToString();
            source.CustomProperties["IsVideo"] = isVideo;

            var musicPropsTask = file.Properties.GetMusicPropertiesAsync();
            var thumbnailTask = file.GetThumbnailAsync(ThumbnailMode.MusicView, 200);

            await Task.WhenAll(musicPropsTask.AsTask(), thumbnailTask.AsTask());

            var musicProps = musicPropsTask.GetResults();
            using var thumbnail = thumbnailTask.GetResults();

            BitmapImage? bitmap = null;
            if (thumbnail is not null)
            {
                bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(thumbnail);
            }

            var playbackItem = new MediaPlaybackItem(source);

            var props = playbackItem.GetDisplayProperties();
            props.Type = isVideo ? MediaPlaybackType.Video : MediaPlaybackType.Music;
            if (isVideo)
                props.VideoProperties.Title = file.DisplayName;
            else
            {
                props.MusicProperties.Title = string.IsNullOrWhiteSpace(musicProps.Title) ? file.DisplayName : musicProps.Title;
                props.MusicProperties.Artist = musicProps.Artist;
            }
            playbackItem.ApplyDisplayProperties(props);

            var title = string.IsNullOrWhiteSpace(musicProps.Title) ? file.DisplayName : musicProps.Title;

            return MediaItem.FromLocal(file.Path, title, musicProps.Artist, bitmap, playbackItem);
        }).ToList();


        var tracks = await Task.WhenAll(tasks);

        _dispatcherQueue.TryEnqueue(() =>
        {
            foreach (var track in tracks)
            {
                Playlist.Add(track);
                _mediaPlaybackList.Items.Add(track.PlaybackItem);
            }

            if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None)
                MediaPlayer.Play();
        });
    }

    public void AddAndPlayRadio(RadioItem station)
    {
        var binder = new MediaBinder { Token = $"Radio|{station.UrlResolved}" };
        binder.Binding += Binder_Binding;

        var source = MediaSource.CreateFromMediaBinder(binder);
        source.CustomProperties["Kind"] = SourceKind.Radio.ToString();
        source.CustomProperties["IsVideo"] = false;

        var playbackItem = new MediaPlaybackItem(source);

        var props = playbackItem.GetDisplayProperties();
        props.Type = MediaPlaybackType.Music;
        props.MusicProperties.Title = station.Name;
        props.MusicProperties.Artist = "Radio";
        playbackItem.ApplyDisplayProperties(props);

        var track = MediaItem.FromRadio(station, playbackItem);

        Playlist.Add(track);
        _mediaPlaybackList.Items.Add(track.PlaybackItem);

        var index = _mediaPlaybackList.Items.IndexOf(track.PlaybackItem);
        _mediaPlaybackList.MoveTo((uint) index);
        MediaPlayer.Play();
    }

    private async void Binder_Binding(MediaBinder sender, MediaBindingEventArgs args)
    {
        var deferral = args.GetDeferral();
        var parts = sender.Token.Split('|');
        var kind = parts[0];
        var url = parts[1];

        try
        {
            if (kind == "Radio")
            {
                bool isHls = url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase);

                if (isHls)
                {
                    using var httpClient = new Windows.Web.Http.HttpClient();
                    httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Novatune/1.0");

                    var amsResult = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(url), httpClient);
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
            else if (kind == "YouTube")
            {
                // var ytUrl = await YoutubeExplode.GetMuxedUrl(url);
                // args.SetUri(new Uri(ytUrl));
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
            return;

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
            var prev = Playlist.FirstOrDefault(t => t.IsCurrent);
            prev?.IsCurrent = false;

            if (args.NewItem is null)
            {
                Title = string.Empty;
                CurrentImage = _defaultImage;
                PlaylistIndex = -1;
                IsLive = false;
                return;
            }

            var kind = args.NewItem.Source.CustomProperties["Kind"] as string ?? "Unknown";
            IsLive = (kind == SourceKind.Radio.ToString());

            var currentTrack = Playlist.FirstOrDefault(t => t.PlaybackItem == args.NewItem);
            if (currentTrack is not null)
            {
                currentTrack.IsCurrent = true;
                Title = currentTrack.Title;
                CurrentImage = currentTrack.Thumbnail ?? _defaultImage;
                PlaylistIndex = Playlist.IndexOf(currentTrack);
            }
            else
            {
                PlaylistIndex = -1;
            }
        });
    }

    private void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
    {
        Debug.WriteLine($"ItemFailed: {args.Error.ErrorCode}");
        _dispatcherQueue.TryEnqueue(() => { if (sender.Items.Count > 1) sender.MoveNext(); });
    }

    #endregion
}
