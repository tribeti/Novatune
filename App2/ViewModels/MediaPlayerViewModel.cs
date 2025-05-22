using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace App2.ViewModels
{
    public partial class MediaPlayerViewModel : ObservableObject
    {
        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private Media _currentMediaTrack;
        private readonly DispatcherQueue _dispatcherQueue;
        private static bool _isLibVLCSharpCoreInitialized = false;

        public ObservableCollection<IStorageItem> MediaItems { get; } = new ObservableCollection<IStorageItem>();

        [ObservableProperty]
        private StorageFile _currentFile;

        [ObservableProperty]
        private string _nowPlayingTitle = "Không có file nào đang phát";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PlayPauseGlyph))]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _isMediaPlayerElementVisible; // For video display elements

        [ObservableProperty]
        private TimeSpan _currentPosition;

        [ObservableProperty]
        private TimeSpan _totalDuration;

        public string PlayPauseGlyph => IsPlaying ? "\uE769" : "\uE768"; // Segoe Fluent Icons: Pause : Play

        public event Action PlaybackStateChanged; // For MediaControlsView to react

        // Expose MediaPlayer instance if VideoView or other LibVLCSharp controls need it directly
        // However, it's often better to encapsulate LibVLCSharp details within the ViewModel.
        public LibVLCSharp.Shared.MediaPlayer PlayerInstance => _mediaPlayer;


        public MediaPlayerViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Cannot get DispatcherQueue for current thread.");

            if (!_isLibVLCSharpCoreInitialized)
            {
                try
                {
                    Core.Initialize(); // LibVLCSharp.Shared.Core
                    _isLibVLCSharpCoreInitialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fatal Error initializing LibVLCSharp.Shared.Core: {ex.Message}");
                    // Optionally, set a ViewModel state indicating an error, or rethrow
                    throw; // Or handle more gracefully depending on application requirements
                }
            }
            InitializeLibVLCAndPlayer();
        }

        private void InitializeLibVLCAndPlayer()
        {
            _libVLC = new LibVLC(); // Consider "--no-video" or other options if only audio
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            // Subscribe to MediaPlayer events
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
            _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
        }

        private void MediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                System.Diagnostics.Debug.WriteLine("LibVLC MediaPlayer encountered an error.");
                NowPlayingTitle = "Lỗi trình phát";
                IsPlaying = false;
                CurrentPosition = TimeSpan.Zero;
                // Consider more specific error handling or user notification
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            });
        }


        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => TotalDuration = TimeSpan.FromMilliseconds(e.Length));
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => CurrentPosition = TimeSpan.FromMilliseconds(e.Time));
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = false;
                CurrentPosition = TimeSpan.Zero;
                // TotalDuration might remain to show length of last played item, or reset:
                // TotalDuration = TimeSpan.Zero; 
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            });
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = false;
                PlaybackStateChanged?.Invoke();
                // Commands usually don't change state on pause/play, but IsPlaying does
            });
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = true;
                PlaybackStateChanged?.Invoke();
            });
        }

        private async void MediaPlayer_EndReached(object sender, EventArgs e) // Note: async void for event handler
        {
            // This needs to run on dispatcher queue for UI interaction (like auto-playing next)
            _dispatcherQueue.TryEnqueue(async () =>
            {
                IsPlaying = false;
                CurrentPosition = TotalDuration; // Or TimeSpan.Zero;
                PlaybackStateChanged?.Invoke();

                if (CanSkipNext()) // Check if SkipNext command can execute
                {
                    await SkipNextAsync(); // Await the async command
                }
                else
                {
                    // No next track, so stop or loop, etc.
                    // For now, just ensure state is fully stopped.
                    StopPlaybackInternal(); // Call internal stop without UI notification if EndReached handles it
                }
                UpdateCommandStates();
            });
        }

        public async Task LoadMediaItemsAsync(StorageFolder folder)
        {
            if (folder == null) return;

            var multimediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".wav", ".aac", ".flac", ".wma", ".ogg", // Audio
                ".mp4", ".mkv", ".avi", ".mov", ".wmv"           // Video
            };

            // Clear previous items and state
            MediaItems.Clear();
            CurrentFile = null;
            NowPlayingTitle = "Không có file nào đang phát";
            TotalDuration = TimeSpan.Zero;
            CurrentPosition = TimeSpan.Zero;
            IsPlaying = false;
            IsMediaPlayerElementVisible = false;


            try
            {
                var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, multimediaExtensions.ToList())
                {
                    FolderDepth = FolderDepth.Deep
                };
                // queryOptions.SortOrder.Clear(); // OrderByName already implies sorting by name
                // queryOptions.SortOrder.Add(new SortEntry { PropertyName = "System.ItemName", AscendingOrder = true });

                var queryResult = folder.CreateItemQueryWithOptions(queryOptions);
                var items = await queryResult.GetItemsAsync();

                foreach (var item in items)
                {
                    // Check if it's a file and has one of the supported extensions
                    if (item is StorageFile file && multimediaExtensions.Contains(file.FileType.ToLowerInvariant()))
                    {
                        MediaItems.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading media items from {folder.Path}: {ex.Message}");
                // Optionally, notify the user or set an error state
            }
            finally
            {
                PlaybackStateChanged?.Invoke(); // Notify UI that items might have loaded (or failed)
                UpdateCommandStates();
            }
        }

        [RelayCommand(CanExecute = nameof(CanPlayMediaFile))]
        public async Task PlayMediaFileAsync(StorageFile file)
        {
            if (file == null) return;

            StopPlaybackInternal(); // Stop current playback and release media

            try
            {
                CurrentFile = file;
                NowPlayingTitle = Path.GetFileNameWithoutExtension(file.Name);
                IsMediaPlayerElementVisible = IsVideoFileExtension(file.FileType);

                _currentMediaTrack = new Media(_libVLC, file.Path, FromType.FromPath);
                // Optional: Parse media for metadata, helps with accurate duration quickly.
                // await _currentMediaTrack.Parse(MediaParseOptions.ParseNetwork); // Or ParseLocal, or default.

                _mediaPlayer.Media = _currentMediaTrack;
                bool success = _mediaPlayer.Play();
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"MediaPlayer.Play() failed for {file.Path}");
                    // Handle error: reset state, notify user
                    CurrentFile = null;
                    NowPlayingTitle = "Lỗi khi phát file";
                    IsPlaying = false;
                }
                // IsPlaying state will be updated by the MediaPlayer_Playing event if successful
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error preparing media file {file.Path}: {ex.Message}");
                CurrentFile = null;
                NowPlayingTitle = "Lỗi khi chuẩn bị file";
                IsPlaying = false;
            }
            finally
            {
                PlaybackStateChanged?.Invoke(); // Update UI state
                UpdateCommandStates();
            }
        }
        private bool CanPlayMediaFile(StorageFile file) => file != null;


        private bool IsVideoFileExtension(string fileExtension)
        {
            var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp4", ".mkv", ".avi", ".mov", ".wmv"
            };
            return videoExtensions.Contains(fileExtension.ToLowerInvariant());
        }

        [RelayCommand(CanExecute = nameof(CanTogglePlayPause))]
        public void TogglePlayPause()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;

            if (_mediaPlayer.State == VLCState.Playing)
            {
                _mediaPlayer.Pause();
            }
            else // Paused, Stopped, Ended, Error, etc.
            {
                _mediaPlayer.Play(); // Attempt to play
            }
            // IsPlaying state updated by events
        }
        private bool CanTogglePlayPause() => _mediaPlayer != null && _mediaPlayer.Media != null && CurrentFile != null;

        private void StopPlaybackInternal() // Internal helper, does not invoke PlaybackStateChanged directly
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying || _mediaPlayer.State == VLCState.Paused) // Check if it's actually playing or paused
                {
                    _mediaPlayer.Stop(); // This will trigger MediaPlayer_Stopped event
                }
            }
            _currentMediaTrack?.Dispose(); // Dispose of the media object
            _currentMediaTrack = null;
        }


        [RelayCommand(CanExecute = nameof(CanStopPlayback))]
        public void StopPlayback()
        {
            StopPlaybackInternal(); // Use internal helper
            // MediaPlayer_Stopped event will handle UI updates like IsPlaying and CurrentPosition.
            // Explicitly ensure title reflects no playback if needed here or in event handler
            // NowPlayingTitle = "Không có file nào đang phát";
            // CurrentFile = null; // Optionally clear current file on explicit stop
            PlaybackStateChanged?.Invoke(); // Ensure UI updates immediately after explicit stop
            UpdateCommandStates();
        }
        private bool CanStopPlayback() => _mediaPlayer != null && _mediaPlayer.Media != null && CurrentFile != null;


        [RelayCommand(CanExecute = nameof(CanSeekPosition))]
        public void Seek(TimeSpan position)
        {
            if (_mediaPlayer != null && _mediaPlayer.Media != null && _mediaPlayer.IsSeekable)
            {
                _mediaPlayer.Time = (long)position.TotalMilliseconds;
            }
        }
        private bool CanSeekPosition(TimeSpan position) => _mediaPlayer != null && _mediaPlayer.Media != null && _mediaPlayer.IsSeekable && CurrentFile != null;


        private int GetCurrentFileIndex()
        {
            if (CurrentFile == null || !MediaItems.Any()) return -1;
            // Find by path as StorageFile instances might differ
            return MediaItems.ToList().FindIndex(item => item is StorageFile sf && sf.Path.Equals(CurrentFile.Path, StringComparison.OrdinalIgnoreCase));
        }

        [RelayCommand(CanExecute = nameof(CanSkipPrevious))]
        public async Task SkipPreviousAsync()
        {
            int currentIndex = GetCurrentFileIndex();
            if (currentIndex > 0 && MediaItems[currentIndex - 1] is StorageFile prevFile)
            {
                await PlayMediaFileAsync(prevFile);
            }
        }
        private bool CanSkipPrevious()
        {
            if (CurrentFile == null || MediaItems.Count <= 1) return false;
            return GetCurrentFileIndex() > 0;
        }

        [RelayCommand(CanExecute = nameof(CanSkipNext))]
        public async Task SkipNextAsync()
        {
            int currentIndex = GetCurrentFileIndex();
            if (currentIndex >= 0 && currentIndex < MediaItems.Count - 1 && MediaItems[currentIndex + 1] is StorageFile nextFile)
            {
                await PlayMediaFileAsync(nextFile);
            }
        }
        private bool CanSkipNext()
        {
            if (CurrentFile == null || MediaItems.Count <= 1) return false;
            int currentIndex = GetCurrentFileIndex();
            return currentIndex >= 0 && currentIndex < MediaItems.Count - 1;
        }

        private void UpdateCommandStates()
        {
            // Notify CanExecute changed for all relevant commands
            // This ensures UI buttons enable/disable correctly based on state
            _dispatcherQueue.TryEnqueue(() =>
            {
                PlayMediaFileCommand.NotifyCanExecuteChanged();
                TogglePlayPauseCommand.NotifyCanExecuteChanged();
                StopPlaybackCommand.NotifyCanExecuteChanged();
                SeekCommand.NotifyCanExecuteChanged(); // Assuming Seek takes a parameter, its CanExecute might not need this often
                SkipPreviousCommand.NotifyCanExecuteChanged();
                SkipNextCommand.NotifyCanExecuteChanged();
            });
        }

        public void Cleanup() // Call when ViewModel is no longer needed
        {
            StopPlaybackInternal(); // Ensure media is stopped and released

            if (_mediaPlayer != null)
            {
                _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                _mediaPlayer.Playing -= MediaPlayer_Playing;
                _mediaPlayer.Paused -= MediaPlayer_Paused;
                _mediaPlayer.Stopped -= MediaPlayer_Stopped;
                _mediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
                _mediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;
                _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }

            _currentMediaTrack?.Dispose(); // Already handled by StopPlaybackInternal but good to be sure
            _currentMediaTrack = null;

            _libVLC?.Dispose();
            _libVLC = null;

            MediaItems.Clear(); // Clear the collection
            System.Diagnostics.Debug.WriteLine("MediaPlayerViewModel Cleaned up.");
        }
    }
}