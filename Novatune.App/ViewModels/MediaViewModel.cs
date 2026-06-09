using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
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
    public MediaSource? mediaSource;
    MediaPlaybackList mediaPlaybackList = new();
    public MediaPlayer mediaPlayer = new();
    public ObservableCollection<StorageFile> MediaFiles { get; } = new();

    [ObservableProperty]
    public partial bool IsPlaying { get; set; } = false;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public MediaViewModel()
    {
        mediaPlayer.PlaybackSession.PlaybackStateChanged += (s, _) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = s.PlaybackState == MediaPlaybackState.Playing;
            });
        };
        mediaPlaybackList.MaxPlayedItemsToKeepOpen = 3;
        mediaPlayer.Source = mediaPlaybackList;
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
                var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
                var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(storageFile));
                mediaPlaybackList.Items.Add(mediaPlaybackItem);
                MediaFiles.Add(file);
            }
        }
    }

    [RelayCommand]
    public void Previous() => mediaPlaybackList.MovePrevious();

    [RelayCommand]
    public void Next() => mediaPlaybackList.MoveNext();


    [RelayCommand]
    public void Shuffle()
    {
        mediaPlaybackList.ShuffleEnabled = !mediaPlaybackList.ShuffleEnabled;

        //DispatcherQueue.TryEnqueue(() =>
        //{
        //    Shuffle_Btn.FontWeight = mediaPlaybackList.ShuffleEnabled ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Light;
        //});
    }

    [RelayCommand]
    public void Repeat()
    {
        mediaPlaybackList.AutoRepeatEnabled = !mediaPlaybackList.AutoRepeatEnabled;

        //DispatcherQueue.TryEnqueue(() =>
        //{
        //    autoRepeatButton.FontWeight = mediaPlaybackList.AutoRepeatEnabled ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Light;
        //});
    }
}
