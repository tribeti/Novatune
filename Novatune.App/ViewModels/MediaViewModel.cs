using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Novatune.App.ViewModels;

public partial class MediaViewModel : BaseViewModel
{
    private readonly MediaPlaybackList _mediaPlaybackList = new();
    public MediaPlayer mediaPlayer = new();
    public ObservableCollection<StorageFile> MediaFiles { get; } = new();

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

            if (playing == IsPlaying)
                return;

            _dispatcherQueue.TryEnqueue(() =>
            {
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
            MediaPosition = session.Position.TotalSeconds;
        };

        _mediaPlaybackList.MaxPlayedItemsToKeepOpen = 3;
        mediaPlayer.Source = _mediaPlaybackList;
    }

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
    public partial double MediaPosition { get; set; } = 0;
    public string PositionText
    {
        get
        {
            var ts = TimeSpan.FromSeconds(MediaPosition);
            return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        }
    }

    [RelayCommand]
    public void PlayPause()
    {
        if (IsPlaying)
            mediaPlayer.Pause();
        else
            mediaPlayer.Play();
    }

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

        if (files is not null)
        {
            foreach (var file in files)
            {
                var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file));
                _mediaPlaybackList.Items.Add(mediaPlaybackItem);
                MediaFiles.Add(file);
            }
        }
    }

    [RelayCommand]
    public void Seek(double seconds)
    {
        mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(seconds);
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
}
