using App2.ViewModels;
using LibVLCSharp.Platforms.Windows;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App2.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FolderDetailPage : Page
    {
        public FolderViewModel ViewModel { get; set; }

        public StorageFolder SelectedFolder { get; set; }
        public ObservableCollection<IStorageItem> FolderItems { get; set; } = new ObservableCollection<IStorageItem>();

        // LibVLC objects
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _currentMedia;
        private StorageFile _currentFile;
        private bool _isPlaying = false;

        public FolderViewModel FolderVM { get; set; }
        public MediaPlayerViewModel MediaPlayerVM { get; set; }


        public FolderDetailPage()
        {
            this.InitializeComponent();
            FolderVM = new FolderViewModel();
            MediaPlayerVM = new MediaPlayerViewModel();
            this.DataContext = MediaPlayerVM;

            // Connect VideoView to media player
            VideoView.MediaPlayer = MediaPlayerVM.Player;

            // Handle playback state changes for UI updates
            MediaPlayerVM.PlaybackStateChanged += MediaPlayerVM_PlaybackStateChanged;
        }

        private void MediaPlayerVM_PlaybackStateChanged()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdatePlayPauseButton();
            });
        }

        private void UpdatePlayPauseButton()
        {
            // Update the play/pause button icon based on playback state
            PlayPauseIcon.Glyph = MediaPlayerVM.IsPlaying ? "\uE769" : "\uE768";
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is StorageFolder folder)
            {
                SelectedFolder = folder;
                await MediaPlayerVM.LoadMediaItemsAsync(folder);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Clean up resources when navigating away
            MediaPlayerVM.PlaybackStateChanged -= MediaPlayerVM_PlaybackStateChanged;
            MediaPlayerVM.Cleanup();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is StorageFile file)
            {
                try
                {
                    await MediaPlayerVM.PlayMediaFileAsync(file);

                    // Update UI visibility
                    NowPlayingSection.Visibility = MediaPlayerVM.IsNowPlayingSectionVisible ? Visibility.Visible : Visibility.Collapsed;
                    MediaPlayerGrid.Visibility = MediaPlayerVM.IsMediaPlayerVisible ? Visibility.Visible : Visibility.Collapsed;

                    // Update play/pause button
                    UpdatePlayPauseButton();
                }
                catch (Exception ex)
                {
                    // Handle any errors
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Lỗi phát media",
                        Content = $"Không thể phát file: {ex.Message}",
                        CloseButtonText = "OK"
                    };

                    await errorDialog.ShowAsync();
                }
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayerVM.TogglePlayPauseCommand.Execute(null);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayerVM.StopPlaybackCommand.Execute(null);
        }
    }
}