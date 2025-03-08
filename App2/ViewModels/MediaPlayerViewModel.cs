using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using System.Linq;
using Microsoft.UI.Dispatching;

namespace App2.ViewModels
{
    public partial class MediaPlayerViewModel : ObservableObject
    {
        public ObservableCollection<IStorageItem> MediaItems { get; } = new ObservableCollection<IStorageItem>();

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _currentMedia;

        [ObservableProperty]
        private StorageFile _currentFile;

        [ObservableProperty]
        private string _nowPlayingTitle = "Không có file nào đang phát";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PlayPauseIcon))]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _isMediaPlayerVisible;

        public string PlayPauseIcon => IsPlaying ? "\uE769" : "\uE768";

        public event Action PlaybackStateChanged;

        public MediaPlayer Player => _mediaPlayer;

        public MediaPlayerViewModel()
        {
            Core.Initialize();
            InitializeLibVLC();
        }

        private void InitializeLibVLC()
        {
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            IsPlaying = false;
            NotifyPlaybackStateChanged();
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            IsPlaying = false;
            NotifyPlaybackStateChanged();
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            IsPlaying = true;
            NotifyPlaybackStateChanged();
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            IsPlaying = false;
            NotifyPlaybackStateChanged();
        }

        private void NotifyPlaybackStateChanged()
        {
            PlaybackStateChanged?.Invoke();
        }

        public async Task LoadMediaItemsAsync(StorageFolder folder)
        {
            if (folder == null) return;

            var multimediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".mp4", ".aac", ".flac", ".wav", ".mkv", ".avi", ".mov", ".ogg"
            };

            MediaItems.Clear();

            try
            {
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading media items: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task PlayMediaFileAsync(StorageFile file)
        {
            if (file == null) return;

            try
            {
                CurrentFile = file;
                NowPlayingTitle = file.Name;
                IsMediaPlayerVisible = IsVideoFile(file.FileType);

                StopPlayback();

                _currentMedia = new Media(_libVLC, file.Path, FromType.FromPath);
                _mediaPlayer.Media = _currentMedia;
                _mediaPlayer.Play();
                IsPlaying = true;

                NotifyPlaybackStateChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing media file: {ex.Message}");
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

                NotifyPlaybackStateChanged();
            }
        }

        [RelayCommand]
        public void StopPlayback()
        {
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }

            if (_currentMedia != null)
            {
                _currentMedia.Dispose();
                _currentMedia = null;
            }

            IsPlaying = false;
            NotifyPlaybackStateChanged();
        }

        public void Cleanup()
        {
            StopPlayback();

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