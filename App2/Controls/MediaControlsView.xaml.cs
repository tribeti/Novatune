using App2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;

namespace App2.Controls
{
    public sealed partial class MediaControlsView : UserControl
    {
        private MediaPlayerViewModel _mediaPlayerViewModel;

        public MediaControlsView()
        {
            this.InitializeComponent();
        }

        public void Initialize(MediaPlayerViewModel viewModel)
        {
            _mediaPlayerViewModel = viewModel;
            _mediaPlayerViewModel.PlaybackStateChanged += MediaPlayerViewModel_PlaybackStateChanged;

            UpdateControlsVisibility();
        }

        private void MediaPlayerViewModel_PlaybackStateChanged()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateControlsVisibility();
            });
        }

        private void UpdateControlsVisibility()
        {
            if (_mediaPlayerViewModel == null)
                return;

            // Cập nhật tiêu đề
            if (_mediaPlayerViewModel.CurrentFile != null)
            {
                MediaTitleText.Text = _mediaPlayerViewModel.CurrentFile.Name;
                MediaPathText.Text = Path.GetDirectoryName(_mediaPlayerViewModel.CurrentFile.Path);
            }
            else
            {
                MediaTitleText.Text = "Không có file nào đang phát";
                MediaPathText.Text = "Chọn một file để phát";
            }

            PlayPauseIcon.Glyph = _mediaPlayerViewModel.IsPlaying ? "\uE769" : "\uE768";
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayerViewModel?.TogglePlayPauseCommand.Execute(null);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayerViewModel?.StopPlaybackCommand.Execute(null);
        }

        public void Cleanup()
        {
            if (_mediaPlayerViewModel != null)
            {
                _mediaPlayerViewModel.PlaybackStateChanged -= MediaPlayerViewModel_PlaybackStateChanged;
            }
        }
    }
}