using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using System.Linq;

namespace App2.ViewModels
{
    public partial class MediaPlayerViewModel : ObservableObject
    {
        // Collection for media files
        public ObservableCollection<IStorageItem> MediaItems { get; } = new ObservableCollection<IStorageItem>();

        // LibVLC objects
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _currentMedia;
        
        // Current file and UI properties
        [ObservableProperty]
        private StorageFile _currentFile;
        
        [ObservableProperty]
        private string _nowPlayingTitle;
        
        [ObservableProperty]
        private bool _isPlaying;
        
        [ObservableProperty]
        private bool _isMediaPlayerVisible;
        
        [ObservableProperty]
        private bool _isNowPlayingSectionVisible;

        // Event handler for UI thread updates
        public event Action PlaybackStateChanged;

        public MediaPlayer Player => _mediaPlayer;

        public MediaPlayerViewModel()
        {
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

            // Handle player events
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            IsPlaying = false;
            PlaybackStateChanged?.Invoke();
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            IsPlaying = false;
            PlaybackStateChanged?.Invoke();
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            IsPlaying = true;
            PlaybackStateChanged?.Invoke();
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            IsPlaying = false;
            PlaybackStateChanged?.Invoke();
        }

        public async Task LoadMediaItemsAsync(StorageFolder folder)
        {
            var multimediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".mp4", ".aac", ".flac", ".wav", ".mkv", ".avi", ".mov", ".ogg"
            };

            MediaItems.Clear();

            var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, multimediaExtensions.ToList());
            queryOptions.FolderDepth = FolderDepth.Deep;

            var query = folder.CreateItemQueryWithOptions(queryOptions);
            var items = await query.GetItemsAsync();

            foreach (var item in items)
            {
                if (item is StorageFile file && multimediaExtensions.Contains(file.FileType))
                {
                    MediaItems.Add(item);
                }
            }
        }

        [RelayCommand]
        public async Task PlayMediaFileAsync(StorageFile file)
        {
            try
            {
                // Store the current file
                CurrentFile = file;

                // Update the UI
                NowPlayingTitle = file.Name;
                IsNowPlayingSectionVisible = true;

                // Determine if this is an audio or video file
                IsMediaPlayerVisible = IsVideoFile(file.FileType);

                // Stop any current playback
                StopPlayback();

                // Create a new media from the file
                string filePath = file.Path;

                // Creating media from path
                _currentMedia = new Media(_libVLC, filePath, FromType.FromPath);

                // Start playback
                _mediaPlayer.Media = _currentMedia;
                _mediaPlayer.Play();
                IsPlaying = true;
            }
            catch (Exception)
            {
                throw;
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

        [RelayCommand]
        public void TogglePlayPause()
        {
            if (_mediaPlayer != null)
            {
                if (IsPlaying)
                {
                    _mediaPlayer.Pause();
                }
                else
                {
                    _mediaPlayer.Play();
                }
            }
        }

        [RelayCommand]
        public void StopPlayback()
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

            IsPlaying = false;
        }

        public void Cleanup()
        {
            // Stop playback
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
    }
}