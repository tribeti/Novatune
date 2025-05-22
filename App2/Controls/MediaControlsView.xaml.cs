using App2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using Windows.Storage;

namespace App2.Controls
{
    public sealed partial class MediaControlsView : UserControl
    {
        private MediaPlayerViewModel _mediaPlayerViewModel;
        private bool _isUserDraggingSlider = false;

        private DispatcherTimer _autoHideTimer;
        private DispatcherTimer _updateTimer; // Timer for real-time updates
        private bool _isPointerOverRootLayout = false;
        private bool _isAutoHideFeatureEnabled = true; // Default value
        public const string AutoHideSettingKey = "MediaControlsAutoHideEnabled";

        public MediaControlsView()
        {
            this.InitializeComponent();
            TimeSlider.IsEnabled = false; // Initially disabled
            LoadAutoHideSetting();

            // Initialize update timer for real-time position updates
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _updateTimer.Tick += UpdateTimer_Tick;

            if (_isAutoHideFeatureEnabled)
            {
                _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) }; // Configurable hide delay
                _autoHideTimer.Tick += AutoHideTimer_Tick;
                DispatcherQueue.TryEnqueue(() => VisualStateManager.GoToState(this, "HiddenState", false));
            }
            else
            {
                DispatcherQueue.TryEnqueue(() => VisualStateManager.GoToState(this, "VisibleState", false));
            }
        }

        private void LoadAutoHideSetting()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(AutoHideSettingKey, out object settingValue) && settingValue is bool value)
            {
                _isAutoHideFeatureEnabled = value;
            }
            else
            {
                // If setting doesn't exist, save the default value
                localSettings.Values[AutoHideSettingKey] = _isAutoHideFeatureEnabled;
            }
        }

        public void UpdateAutoHideFeatureState(bool isEnabled)
        {
            if (_isAutoHideFeatureEnabled == isEnabled)
            {
                return; // No change
            }

            _isAutoHideFeatureEnabled = isEnabled;
            // The setting itself is saved by SettingsPage

            if (_isAutoHideFeatureEnabled)
            {
                if (_autoHideTimer == null)
                {
                    _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    _autoHideTimer.Tick += AutoHideTimer_Tick;
                }
                // If pointer is not over, attempt to hide (timer will handle delay if needed)
                if (!_isPointerOverRootLayout)
                {
                    // If something is playing, start timer, otherwise hide immediately
                    if (_mediaPlayerViewModel != null && _mediaPlayerViewModel.IsPlaying)
                        _autoHideTimer.Start();
                    else
                        HideControls();
                }
            }
            else // Auto-hide disabled
            {
                _autoHideTimer?.Stop();
                ShowControls(); // Make sure controls are visible
            }
        }

        public void Initialize(MediaPlayerViewModel viewModel)
        {
            _mediaPlayerViewModel = viewModel;

            if (_mediaPlayerViewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("Error: MediaPlayerViewModel is null in MediaControlsView.Initialize.");
                UpdateControlsAppearance();
                return;
            }

            _mediaPlayerViewModel.PlaybackStateChanged -= MediaPlayerViewModel_PlaybackStateChanged; // Ensure no multiple subscriptions
            _mediaPlayerViewModel.PlaybackStateChanged += MediaPlayerViewModel_PlaybackStateChanged;

            TimeSlider.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TimeSlider_PointerPressed), true);
            TimeSlider.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(TimeSlider_PointerReleased), true);

            UpdateControlsAppearance();
            ApplyInitialAutoHideState();
        }

        // Optional: If PlaybackStateChanged is not granular enough for CurrentPosition/TotalDuration
        // private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        // {
        //    if (e.PropertyName == nameof(MediaPlayerViewModel.CurrentPosition) ||
        //        e.PropertyName == nameof(MediaPlayerViewModel.TotalDuration))
        //    {
        //        DispatcherQueue.TryEnqueue(() => UpdateSliderAndTimeTexts());
        //    }
        // }

        private void ApplyInitialAutoHideState()
        {
            if (_isAutoHideFeatureEnabled)
            {
                bool shouldHide = !_isPointerOverRootLayout &&
                                  (_mediaPlayerViewModel == null || !_mediaPlayerViewModel.IsPlaying || _mediaPlayerViewModel.CurrentFile == null);
                if (shouldHide)
                {
                    HideControls();
                }
                else
                {
                    ShowControls();
                    if (_mediaPlayerViewModel != null && _mediaPlayerViewModel.IsPlaying && !_isPointerOverRootLayout)
                    {
                        _autoHideTimer?.Start();
                    }
                }
            }
            else
            {
                ShowControls(); // Always show if auto-hide is off
            }
        }

        private void MediaPlayerViewModel_PlaybackStateChanged()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateControlsAppearance();

                if (_mediaPlayerViewModel != null && _mediaPlayerViewModel.IsPlaying)
                {
                    _updateTimer.Start();
                }
                else
                {
                    _updateTimer.Stop();
                }

                if (_isAutoHideFeatureEnabled)
                {
                    if (_mediaPlayerViewModel != null && _mediaPlayerViewModel.IsPlaying)
                    {
                        ShowControls();
                        if (!_isPointerOverRootLayout)
                        {
                            _autoHideTimer?.Start(); // Start timer to hide if pointer is not over
                        }
                    }
                    else if (_mediaPlayerViewModel != null && !_mediaPlayerViewModel.IsPlaying && !_isPointerOverRootLayout)
                    {
                        HideControls();
                    }
                }
                else
                {
                    ShowControls();
                }
            });
        }

        private void UpdateTimer_Tick(object sender, object e)
        {
            if (_mediaPlayerViewModel != null && _mediaPlayerViewModel.IsPlaying && !_isUserDraggingSlider)
            {
                DispatcherQueue.TryEnqueue(() => UpdateSliderAndTimeTexts(true));
            }
        }

        private void UpdateControlsAppearance()
        {
            if (_mediaPlayerViewModel == null || _mediaPlayerViewModel.CurrentFile == null)
            {
                MediaTitleText.Text = "Không có file nào đang phát";
                PlayPauseIcon.Glyph = "\uE768";
                UpdateSliderAndTimeTexts(false);
                SetButtonsEnabled(false);
                return;
            }

            MediaTitleText.Text = Path.GetFileNameWithoutExtension(_mediaPlayerViewModel.CurrentFile.Name);
            string directoryPath = Path.GetDirectoryName(_mediaPlayerViewModel.CurrentFile.Path);
            PlayPauseIcon.Glyph = _mediaPlayerViewModel.IsPlaying ? "\uE769" : "\uE768"; // Pause : Play

            UpdateSliderAndTimeTexts(true);
            SetButtonsEnabled(true);
        }

        private void SetButtonsEnabled(bool isEnabled)
        {
            if (_mediaPlayerViewModel == null) isEnabled = false;

            PlayPauseButton.IsEnabled = isEnabled && (_mediaPlayerViewModel?.TogglePlayPauseCommand.CanExecute(null) ?? false);
            StopButton.IsEnabled = isEnabled && (_mediaPlayerViewModel?.StopPlaybackCommand.CanExecute(null) ?? false);
            PreviousButton.IsEnabled = isEnabled && (_mediaPlayerViewModel?.SkipPreviousCommand.CanExecute(null) ?? false);
            NextButton.IsEnabled = isEnabled && (_mediaPlayerViewModel?.SkipNextCommand.CanExecute(null) ?? false);
            TimeSlider.IsEnabled = isEnabled && (_mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) ?? false);
            SeekBackwardButton.IsEnabled = isEnabled && (_mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) ?? false);
            SeekForwardButton.IsEnabled = isEnabled && (_mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) ?? false);
        }

        private void UpdateSliderAndTimeTexts(bool hasMedia = true)
        {
            if (!hasMedia || _mediaPlayerViewModel == null)
            {
                CurrentTimeText.Text = FormatTimeSpan(TimeSpan.Zero);
                TotalTimeText.Text = FormatTimeSpan(TimeSpan.Zero);
                if (!_isUserDraggingSlider) TimeSlider.Value = 0;
                TimeSlider.Maximum = 1;
                return;
            }

            var totalDuration = _mediaPlayerViewModel.TotalDuration;
            var currentPosition = _mediaPlayerViewModel.CurrentPosition;

            TimeSlider.Maximum = totalDuration.TotalSeconds > 0 ? totalDuration.TotalSeconds : 1;
            if (!_isUserDraggingSlider)
            {
                TimeSlider.Value = Math.Min(currentPosition.TotalSeconds, TimeSlider.Maximum);
            }

            CurrentTimeText.Text = FormatTimeSpan(currentPosition);
            TotalTimeText.Text = FormatTimeSpan(totalDuration);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return timeSpan.TotalHours >= 1 ? timeSpan.ToString(@"h\:mm\:ss") : timeSpan.ToString(@"m\:ss");
        }

        private void RootLayoutForAutoHide_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverRootLayout = true;
            if (_isAutoHideFeatureEnabled)
            {
                _autoHideTimer?.Stop();
                ShowControls();
            }
        }

        private void RootLayoutForAutoHide_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverRootLayout = false;
            if (_isAutoHideFeatureEnabled)
            {
                if (_mediaPlayerViewModel != null && _mediaPlayerViewModel.CurrentFile != null)
                {
                    _autoHideTimer?.Start();
                }
                else
                {
                    HideControls();
                }
            }
        }

        private void AutoHideTimer_Tick(object sender, object e)
        {
            _autoHideTimer?.Stop();
            if (_isAutoHideFeatureEnabled && !_isPointerOverRootLayout)
            {
                HideControls();
            }
        }

        private void ShowControls()
        {
            DispatcherQueue.TryEnqueue(() => VisualStateManager.GoToState(this, "VisibleState", true));
        }

        private void HideControls()
        {
            DispatcherQueue.TryEnqueue(() => VisualStateManager.GoToState(this, "HiddenState", true));
        }

        private void TimeSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isUserDraggingSlider = true;
        }

        private void TimeSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isUserDraggingSlider)
            {
                _isUserDraggingSlider = false;
                TimeSpan seekPosition = TimeSpan.FromSeconds(TimeSlider.Value);
                if (_mediaPlayerViewModel?.SeekCommand.CanExecute(seekPosition) == true)
                {
                    _mediaPlayerViewModel.SeekCommand.Execute(seekPosition);
                }
            }
        }

        private void SeekBackwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) == true)
            {
                var currentPosition = _mediaPlayerViewModel.CurrentPosition;
                var newPosition = TimeSpan.FromSeconds(Math.Max(0, currentPosition.TotalSeconds - 5));
                _mediaPlayerViewModel.SeekCommand.Execute(newPosition);
            }
        }

        private void SeekForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayerViewModel?.SeekCommand.CanExecute(TimeSpan.Zero) == true)
            {
                var currentPosition = _mediaPlayerViewModel.CurrentPosition;
                var totalDuration = _mediaPlayerViewModel.TotalDuration;
                var newPosition = TimeSpan.FromSeconds(Math.Min(totalDuration.TotalSeconds, currentPosition.TotalSeconds + 5));
                _mediaPlayerViewModel.SeekCommand.Execute(newPosition);
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) => _mediaPlayerViewModel?.TogglePlayPauseCommand.Execute(null);
        private void StopButton_Click(object sender, RoutedEventArgs e) => _mediaPlayerViewModel?.StopPlaybackCommand.Execute(null);
        private void PreviousButton_Click(object sender, RoutedEventArgs e) => _mediaPlayerViewModel?.SkipPreviousCommand.Execute(null);
        private void NextButton_Click(object sender, RoutedEventArgs e) => _mediaPlayerViewModel?.SkipNextCommand.Execute(null);

        public void Cleanup()
        {
            if (_mediaPlayerViewModel != null)
            {
                _mediaPlayerViewModel.PlaybackStateChanged -= MediaPlayerViewModel_PlaybackStateChanged;
                // Unsubscribe from PropertyChanged if subscribed
                // _mediaPlayerViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _mediaPlayerViewModel = null;
            }

            _autoHideTimer?.Stop();
            _autoHideTimer = null;

            _updateTimer?.Stop();
            _updateTimer = null;
        }
    }
}