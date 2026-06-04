using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Windows.Media.Playback;

namespace Novatune.App.ViewModels;

public partial class MediaViewModel : BaseViewModel
{
    public MediaPlayer Player = new();

    [ObservableProperty]
    public partial bool IsPlaying { get; set; } = false;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public MediaViewModel()
    {
        Player.PlaybackSession.PlaybackStateChanged += (s, _) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = s.PlaybackState == MediaPlaybackState.Playing;
            });
        };
    }

    [RelayCommand]
    public void PlayPause()
    {
        if (IsPlaying)
            Player.Pause();
        else
            Player.Play();
    }
}
