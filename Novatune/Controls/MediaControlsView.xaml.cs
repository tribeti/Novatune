using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Novatune.Enums;
using Novatune.ViewModels;
using System;

namespace Novatune.Controls;

public sealed partial class MediaControlsView : UserControl
{
    private MediaPlayerViewModel? _mediaPlayerViewModel;
    private bool _isUserDraggingSlider = false;
    private DispatcherTimer? _updateTimer;

    public MediaControlsView ()
    {
        this.InitializeComponent();
        TimeSlider.IsEnabled = false;

        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _updateTimer.Tick += UpdateTimer_Tick;
    }

    public void Initialize (MediaPlayerViewModel viewModel)
    {
        _mediaPlayerViewModel = viewModel;

        if ( _mediaPlayerViewModel is null )
        {
            UpdateControlsAppearance();
            return;
        }

        _mediaPlayerViewModel.PlaybackStateChanged -= MediaPlayerViewModel_PlaybackStateChanged;
        _mediaPlayerViewModel.PlaybackStateChanged += MediaPlayerViewModel_PlaybackStateChanged;

        TimeSlider.AddHandler(UIElement.PointerPressedEvent , new PointerEventHandler(TimeSlider_PointerPressed) , true);
        TimeSlider.AddHandler(UIElement.PointerReleasedEvent , new PointerEventHandler(TimeSlider_PointerReleased) , true);

        UpdateControlsAppearance();
    }

    private void MediaPlayerViewModel_PlaybackStateChanged ()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateControlsAppearance();

            if ( _mediaPlayerViewModel is not null && _mediaPlayerViewModel.IsPlaying )
            {
                _updateTimer?.Start();
            }
            else
            {
                _updateTimer?.Stop();
            }
        });
    }

    private void UpdateTimer_Tick (object? sender , object e)
    {
        if ( _mediaPlayerViewModel is not null && _mediaPlayerViewModel.IsPlaying && !_isUserDraggingSlider )
        {
            DispatcherQueue.TryEnqueue(() => UpdateSliderAndTimeTexts(true));
        }
    }

    private void UpdateControlsAppearance ()
    {
        if ( _mediaPlayerViewModel is not null && _mediaPlayerViewModel.CurrentOnlineAudio is not null && _mediaPlayerViewModel.IsPlaying )
        {
            MediaTitleText.Text = _mediaPlayerViewModel.CurrentOnlineAudio.Title; // Hoặc .DisplayTitle
                                                                                  // Artist và Album có thể không có hoặc bạn đặt giá trị mặc định trong OnlineModel
                                                                                  // MediaArtistText.Text = _mediaPlayerViewModel.CurrentOnlineAudio.Author; // Nếu có control này
            PlayPauseIcon.Glyph = _mediaPlayerViewModel.IsPlaying ? "\uE769" : "\uE768";
            RepeatIcon.Glyph = _mediaPlayerViewModel.RepeatGlyph;
            ShuffleIcon.Glyph = _mediaPlayerViewModel.ShuffleGlyph;
            ShuffleOffOverlay.Visibility = _mediaPlayerViewModel.ShuffleMode == MediaEnums.ShuffleMode.Off ? Visibility.Visible : Visibility.Collapsed;
            UpdateSliderAndTimeTexts(true);
            SetButtonsEnabled(true);
            return;
        }
        else if ( _mediaPlayerViewModel is not null && _mediaPlayerViewModel.CurrentAudio is not null )
        {
            MediaTitleText.Text = _mediaPlayerViewModel.CurrentAudio.DisplayTitle;

            PlayPauseIcon.Glyph = _mediaPlayerViewModel.IsPlaying ? "\uE769" : "\uE768";
            RepeatIcon.Glyph = _mediaPlayerViewModel.RepeatGlyph;
            ShuffleIcon.Glyph = _mediaPlayerViewModel.ShuffleGlyph;
            ShuffleOffOverlay.Visibility = _mediaPlayerViewModel.ShuffleMode == MediaEnums.ShuffleMode.Off ? Visibility.Visible : Visibility.Collapsed;
            UpdateSliderAndTimeTexts(true);
            SetButtonsEnabled(true);
            return;
        }
        else if ( _mediaPlayerViewModel is not null )
        {
            MediaTitleText.Text = _mediaPlayerViewModel.NowPlayingTitle;
            PlayPauseIcon.Glyph = "\uE768";
            RepeatIcon.Glyph = _mediaPlayerViewModel.RepeatGlyph;
            ShuffleIcon.Glyph = _mediaPlayerViewModel.ShuffleGlyph;
            ShuffleOffOverlay.Visibility = _mediaPlayerViewModel.ShuffleMode == MediaEnums.ShuffleMode.Off ? Visibility.Visible : Visibility.Collapsed;
            UpdateSliderAndTimeTexts(false);
            SetButtonsEnabled(false);
            TimeSlider.IsEnabled = false;
        }
        else
        {
            MediaTitleText.Text = "No file is currently playing";
            PlayPauseIcon.Glyph = "\uE768";
            UpdateSliderAndTimeTexts(false);
            SetButtonsEnabled(false);
            TimeSlider.IsEnabled = false;
        }
    }

    private void SetButtonsEnabled (bool isEnabled)
    {
        if ( _mediaPlayerViewModel is null )
            isEnabled = false;

        PlayPauseButton.IsEnabled = isEnabled && ( _mediaPlayerViewModel?.TogglePlayPauseCommand.CanExecute(null) ?? false );
        StopButton.IsEnabled = isEnabled && ( _mediaPlayerViewModel?.StopPlaybackCommand.CanExecute(null) ?? false );
        PreviousButton.IsEnabled = isEnabled && ( _mediaPlayerViewModel?.SkipPreviousCommand.CanExecute(null) ?? false );
        NextButton.IsEnabled = isEnabled && ( _mediaPlayerViewModel?.SkipNextCommand.CanExecute(null) ?? false );
        TimeSlider.IsEnabled = isEnabled && ( _mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) ?? false );
        SeekBackwardButton.IsEnabled = isEnabled && ( _mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) ?? false );
        SeekForwardButton.IsEnabled = isEnabled && ( _mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) ?? false );
    }

    private void UpdateSliderAndTimeTexts (bool hasMedia = true)
    {
        if ( !hasMedia || _mediaPlayerViewModel is null )
        {
            CurrentTimeText.Text = FormatTimeSpan(TimeSpan.Zero);
            TotalTimeText.Text = FormatTimeSpan(TimeSpan.Zero);
            if ( !_isUserDraggingSlider )
                TimeSlider.Value = 0;
            TimeSlider.Maximum = 1;
            return;
        }

        var totalDuration = _mediaPlayerViewModel.TotalDuration;
        var currentPosition = _mediaPlayerViewModel.CurrentPosition;

        if ( _mediaPlayerViewModel.CurrentOnlineAudio is not null && _mediaPlayerViewModel.IsPlaying )
        {
            totalDuration = _mediaPlayerViewModel.CurrentOnlineAudio.DurationTimeSpan ?? TimeSpan.Zero;
        }
        else if ( _mediaPlayerViewModel.CurrentAudio is not null )
        {
            totalDuration = _mediaPlayerViewModel.TotalDuration;
        }
        else
        {
            totalDuration = TimeSpan.Zero;
        }
        if ( totalDuration == TimeSpan.Zero )
        {
            if ( _mediaPlayerViewModel.CurrentOnlineAudio is not null && _mediaPlayerViewModel.CurrentOnlineAudio.DurationTimeSpan.HasValue )
            {
                totalDuration = _mediaPlayerViewModel.CurrentOnlineAudio.DurationTimeSpan.Value;
            }
            else if ( _mediaPlayerViewModel.CurrentAudio is not null )
            {
                totalDuration = _mediaPlayerViewModel.CurrentAudio.Duration;
            }
        }


        TimeSlider.Maximum = totalDuration.TotalSeconds > 0 ? totalDuration.TotalSeconds : 1;
        if ( !_isUserDraggingSlider )
        {
            TimeSlider.Value = Math.Min(currentPosition.TotalSeconds , TimeSlider.Maximum);
        }

        CurrentTimeText.Text = FormatTimeSpan(currentPosition);
        TotalTimeText.Text = FormatTimeSpan(totalDuration);
    }

    private static string FormatTimeSpan (TimeSpan timeSpan)
    {
        return timeSpan.TotalHours >= 1 ? timeSpan.ToString(@"h\:mm\:ss") : timeSpan.ToString(@"m\:ss");
    }

    private void TimeSlider_PointerPressed (object sender , PointerRoutedEventArgs e)
    {
        _isUserDraggingSlider = true;
    }

    private void TimeSlider_PointerReleased (object sender , PointerRoutedEventArgs e)
    {
        if ( _isUserDraggingSlider )
        {
            _isUserDraggingSlider = false;
            TimeSpan seekPosition = TimeSpan.FromSeconds(TimeSlider.Value);
            if ( _mediaPlayerViewModel?.SeekCommand.CanExecute(seekPosition) == true )
            {
                _mediaPlayerViewModel.SeekCommand.Execute(seekPosition);
            }
        }
    }

    private void SeekBackwardButton_Click (object sender , RoutedEventArgs e)
    {
        if ( _mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) == true )
        {
            var currentPosition = _mediaPlayerViewModel.CurrentPosition;
            var newPosition = TimeSpan.FromSeconds(Math.Max(0 , currentPosition.TotalSeconds - 5));
            _mediaPlayerViewModel.SeekCommand.Execute(newPosition);
        }
    }

    private void SeekForwardButton_Click (object sender , RoutedEventArgs e)
    {
        if ( _mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) == true )
        {
            var currentPosition = _mediaPlayerViewModel.CurrentPosition;
            var totalDuration = _mediaPlayerViewModel.TotalDuration;
            var newPosition = TimeSpan.FromSeconds(Math.Min(totalDuration.TotalSeconds , currentPosition.TotalSeconds + 5));
            _mediaPlayerViewModel.SeekCommand.Execute(newPosition);
        }
    }

    private void PlayPauseButton_Click (object sender , RoutedEventArgs e) => _mediaPlayerViewModel?.TogglePlayPauseCommand.Execute(null);
    private void StopButton_Click (object sender , RoutedEventArgs e) => _mediaPlayerViewModel?.StopPlaybackCommand.Execute(null);
    private void PreviousButton_Click (object sender , RoutedEventArgs e) => _mediaPlayerViewModel?.SkipPreviousCommand.Execute(null);
    private void NextButton_Click (object sender , RoutedEventArgs e) => _mediaPlayerViewModel?.SkipNextCommand.Execute(null);

    private void RepeatButton_Click (object sender , RoutedEventArgs e)
    {
        _mediaPlayerViewModel?.ToggleRepeatModeCommand.Execute(null);
        RepeatIcon.Glyph = _mediaPlayerViewModel?.RepeatGlyph;
    }

    private void ShuffleButton_Click (object sender , RoutedEventArgs e)
    {
        _mediaPlayerViewModel?.ToggleShuffleModeCommand.Execute(null);
        UpdateControlsAppearance();
    }

    public void Cleanup ()
    {
        if ( _mediaPlayerViewModel is not null )
        {
            _mediaPlayerViewModel.PlaybackStateChanged -= MediaPlayerViewModel_PlaybackStateChanged;
            _mediaPlayerViewModel = null;
        }

        _updateTimer?.Stop();
        _updateTimer = null;
    }
}
