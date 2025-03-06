using App2.ViewModels;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;
using System.Linq;
using LibVLCSharp.Platforms.Windows;

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

        public FolderDetailPage()
        {
            this.InitializeComponent();
            ViewModel = new FolderViewModel();
            this.DataContext = ViewModel;

            // Initialize LibVLC
            Core.Initialize();
            InitializeLibVLC();
        }

        private void InitializeLibVLC()
        {
            // Create LibVLC instance with desired options
            _libVLC = new LibVLC();

            // Create media player
            _mediaPlayer = new MediaPlayer(_libVLC);

            // Connect VideoView to media player
            VideoView.MediaPlayer = _mediaPlayer;

            // Handle player events
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isPlaying = false;
                UpdatePlayPauseButton();
            });
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isPlaying = false;
                UpdatePlayPauseButton();
            });
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isPlaying = true;
                UpdatePlayPauseButton();
            });
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isPlaying = false;
                UpdatePlayPauseButton();
            });
        }

        private void UpdatePlayPauseButton()
        {
            // Update the play/pause button icon based on playback state
            PlayPauseIcon.Glyph = _isPlaying ? "\uE769" : "\uE768";
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is StorageFolder folder)
            {
                SelectedFolder = folder;
                await GetFilesInFolderAsync(folder);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Clean up resources when navigating away
            StopPlayback();

            // Dispose of LibVLC resources
            if (_mediaPlayer != null)
            {
                _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                _mediaPlayer.Playing -= MediaPlayer_Playing;
                _mediaPlayer.Paused -= MediaPlayer_Paused;
                _mediaPlayer.Stopped -= MediaPlayer_Stopped;
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }

            if (_currentMedia != null)
            {
                _currentMedia.Dispose();
                _currentMedia = null;
            }

            if (_libVLC != null)
            {
                _libVLC.Dispose();
                _libVLC = null;
            }
        }

        private async Task GetFilesInFolderAsync(StorageFolder folder)
        {
            var multimediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".mp4", ".aac", ".flac", ".wav", ".mkv", ".avi", ".mov", ".ogg"
            };

            FolderItems.Clear();

            var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, multimediaExtensions.ToList());
            queryOptions.FolderDepth = FolderDepth.Deep;

            var query = folder.CreateItemQueryWithOptions(queryOptions);
            var items = await query.GetItemsAsync();

            foreach (var item in items)
            {
                if (item is StorageFile file && multimediaExtensions.Contains(file.FileType))
                {
                    FolderItems.Add(item);
                }
            }
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
                await PlayMediaFileAsync(file);
            }
        }

        private async Task PlayMediaFileAsync(StorageFile file)
        {
            try
            {
                // Store the current file
                _currentFile = file;

                // Update the UI
                NowPlayingTitle.Text = file.Name;
                NowPlayingSection.Visibility = Visibility.Visible;

                // Determine if this is an audio or video file
                bool isVideo = IsVideoFile(file.FileType);
                MediaPlayerGrid.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;

                // Stop any current playback
                StopPlayback();

                // Create a new media from the file
                string filePath = file.Path;

                // Creating media from path (alternative approach using file path)
                _currentMedia = new Media(_libVLC, filePath, FromType.FromPath);

                // Start playback
                _mediaPlayer.Media = _currentMedia;
                _mediaPlayer.Play();
                _isPlaying = true;
                UpdatePlayPauseButton();
            }
            catch (Exception ex)
            {
                // Handle any errors
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "L?i phát media",
                    Content = $"Không th? phát file: {ex.Message}",
                    CloseButtonText = "OK"
                };

                await errorDialog.ShowAsync();
            }
        }

        private bool IsVideoFile(string fileExtension)
        {
            var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp4", ".mkv", ".avi", ".mov", ".wmv"
            };

            return videoExtensions.Contains(fileExtension);
        }

        private void StopPlayback()
        {
            // Stop current playback
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }

            // Clean up current media
            if (_currentMedia != null)
            {
                _currentMedia.Dispose();
                _currentMedia = null;
            }

            _isPlaying = false;
            UpdatePlayPauseButton();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                if (_isPlaying)
                {
                    _mediaPlayer.Pause();
                }
                else
                {
                    _mediaPlayer.Play();
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopPlayback();
        }
    }
}