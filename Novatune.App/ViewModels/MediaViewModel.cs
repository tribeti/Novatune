using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Novatune.App.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Novatune.App.ViewModels;

public partial class MediaViewModel : BaseViewModel
{
    public MediaPlayer mediaPlayer = new();
    private readonly MediaPlaybackList _mediaPlaybackList = new();
    public ObservableCollection<MediaItem> Playlist { get; } = [];
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly DispatcherTimer _positionTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500)
    };

    public MediaViewModel()
    {
        mediaPlayer.PlaybackSession.PlaybackStateChanged += (s, _) =>
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
            var session = mediaPlayer.PlaybackSession;
            MediaDuration = session.NaturalDuration.TotalSeconds;
            PlaybackPosition = session.Position.TotalSeconds;

            if (!IsUserInteracting)
                TimelinePosition = PlaybackPosition;
        };

        _mediaPlaybackList.MaxPlayedItemsToKeepOpen = 3;
        _mediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
        _mediaPlaybackList.ItemFailed += MediaPlaybackList_ItemFailed;
        mediaPlayer.Source = _mediaPlaybackList;
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
        mediaPlayer.Volume = Math.Clamp(value / 100.0, 0.0, 1.0);
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
            mediaPlayer.Pause();
        else
            mediaPlayer.Play();
    }

    [RelayCommand]
    public void Seek(double seconds)
    {
        mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(seconds);
        PlaybackPosition = seconds;
    }

    [RelayCommand]
    public void CommitSeek()
    {
        mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(TimelinePosition);
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
    public partial int PlaylistIndex { get; set; } = 0;

    private readonly BitmapImage _defaultImage = new(new Uri("ms-appx:///Assets/LockScreenLogo.png"));

    [RelayCommand]
    public async Task AddMedia()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List,
            FileTypeFilter = { ".wmv", ".mp4", ".mkv", ".mp3", ".flac" },
        };

        var mainWindow = (App.Current as App)!.MainWindow;
        var hwnd = WindowNative.GetWindowHandle(mainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();

        if (files is not null && files.Count > 0)
        {
            var tasks = files.Select(async file =>
            {
                var source = MediaSource.CreateFromStorageFile(file);
                var playbackItem = new MediaPlaybackItem(source);
                var musicProps = await file.Properties.GetMusicPropertiesAsync();
                using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200);

                var item = new MediaItem
                {
                    PlaybackItem = playbackItem,
                    DisplayName = file.DisplayName,
                    FilePath = file.Path,
                    Title = string.IsNullOrWhiteSpace(musicProps.Title) ? file.DisplayName : musicProps.Title,
                    Artist = musicProps.Artist
                };

                if (thumbnail is not null)
                {
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(thumbnail);
                    item.Img = bitmap;
                }
                return item;
            });

            var items = await Task.WhenAll(tasks);

            foreach (var item in items)
            {
                Playlist.Add(item);
                _mediaPlaybackList.Items.Add(item.PlaybackItem);
            }

            mediaPlayer.Play();
        }
    }

    [RelayCommand]
    public void JumpTo(int index)
    {
        if (index < 0 || index >= Playlist.Count)
            return;

        var selectedItem = Playlist[index];
        var playbackItemIndex = _mediaPlaybackList.Items.IndexOf(selectedItem.PlaybackItem);

        if (playbackItemIndex >= 0)
        {
            _mediaPlaybackList.MoveTo((uint) playbackItemIndex);
        }
    }

    private void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
    {
        var failedItem = args.Item;
        var error = args.Error;

        switch (error.ErrorCode)
        {
            case MediaPlaybackItemErrorCode.NetworkError:
            _dispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine($"Network error: {error.ExtendedError.Message}");
            });
            break;

            case MediaPlaybackItemErrorCode.DecodeError:
            _dispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine("Cannot decode this media file.");
            });
            break;

            case MediaPlaybackItemErrorCode.EncryptionError:
            _dispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine("Decryption/DRM error for this item.");
            });
            break;

            case MediaPlaybackItemErrorCode.SourceNotSupportedError:
            _dispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine("File format is not supported.");
            });
            break;
            default:
            _dispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine($"[MediaPlaybackList] ItemFailed: {error.ErrorCode}, HResult: 0x{error.ExtendedError?.HResult:X8}");
            });
            break;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (sender.Items.Count > 1)
            {
                sender.MoveNext();
            }
        });
    }

    private MediaItem? _currentMediaItem;
    private void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _currentMediaItem?.IsCurrent = false;
            var current = Playlist.FirstOrDefault(x => x.PlaybackItem == args.NewItem);
            current?.IsCurrent = true;

            _currentMediaItem = current;

            Title = current?.Title ?? string.Empty;
            CurrentImage = current?.Img ?? _defaultImage;
            var newIndex = current is not null ? Playlist.IndexOf(current) : -1;
            if (newIndex >= 0 && newIndex != PlaylistIndex)
                PlaylistIndex = newIndex;
        });
    }

    #endregion
}
