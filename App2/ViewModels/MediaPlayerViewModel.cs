using App2.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace App2.ViewModels
{
    public enum RepeatMode
    {
        None,
        One,
        All
    }

    public enum ShuffleMode
    {
        Off,
        On
    }

    public partial class MediaPlayerViewModel : ObservableObject
    {
        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private Media _currentMediaTrack;
        private readonly DispatcherQueue _dispatcherQueue;
        private static bool _isLibVLCSharpCoreInitialized = false;
        private List<LocalAudioModel> _shuffledPlaylist;
        private Random _random = new Random();

        // Collections
        public ObservableCollection<LocalAudioModel> AudioFiles { get; } = new ObservableCollection<LocalAudioModel>();
        public ObservableCollection<LocalAudioModel> FilteredAudioFiles { get; } = new ObservableCollection<LocalAudioModel>();
        public ObservableCollection<LocalAudioModel> FavoriteAudioFiles { get; } = new ObservableCollection<LocalAudioModel>();

        // Current Playing
        [ObservableProperty]
        private LocalAudioModel _currentAudio;

        [ObservableProperty]
        private string _nowPlayingTitle = "Không có file nào đang phát";

        [ObservableProperty]
        private string _nowPlayingArtist = "";

        [ObservableProperty]
        private string _nowPlayingAlbum = "";

        // Playback State
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PlayPauseGlyph))]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _isMediaPlayerElementVisible;

        [ObservableProperty]
        private TimeSpan _currentPosition;

        [ObservableProperty]
        private TimeSpan _totalDuration;

        [ObservableProperty]
        private string _currentPositionString = "0:00";

        [ObservableProperty]
        private string _totalDurationString = "0:00";

        // Playlist Controls
        [ObservableProperty]
        private RepeatMode _repeatMode = RepeatMode.None;

        [ObservableProperty]
        private ShuffleMode _shuffleMode = ShuffleMode.Off;

        [ObservableProperty]
        private int _volume = 300;

        // Search and Filter
        [ObservableProperty]
        private string _searchText = "";

        // UI Properties
        public string PlayPauseGlyph => IsPlaying ? "\uE769" : "\uE768";
        public string RepeatGlyph => RepeatMode switch
        {
            RepeatMode.None => "\uE8EE",
            RepeatMode.One => "\uE8ED",
            RepeatMode.All => "\uE8EE",
            _ => "\uE8EE"
        };
        public string ShuffleGlyph => ShuffleMode == ShuffleMode.On ? "\uE8B1" : "\uE8B1";

        // Events
        public event Action PlaybackStateChanged;
        public LibVLCSharp.Shared.MediaPlayer PlayerInstance => _mediaPlayer;

        public MediaPlayerViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Cannot get DispatcherQueue for current thread.");

            if (!_isLibVLCSharpCoreInitialized)
            {
                try
                {
                    Core.Initialize();
                    _isLibVLCSharpCoreInitialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fatal Error initializing LibVLCSharp.Shared.Core: {ex.Message}");
                    throw;
                }
            }
            InitializeLibVLCAndPlayer();
        }

        private void InitializeLibVLCAndPlayer()
        {
            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
            _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            _mediaPlayer.Volume = Volume;
        }

        private void MediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                NowPlayingTitle = "Lỗi trình phát";
                NowPlayingArtist = "";
                NowPlayingAlbum = "";
                IsPlaying = false;
                CurrentPosition = TimeSpan.Zero;
                if (CurrentAudio != null)
                {
                    CurrentAudio.IsPlaying = false;
                }
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            });
        }

        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                TotalDuration = TimeSpan.FromMilliseconds(e.Length);
                TotalDurationString = FormatDuration(TotalDuration);
            });
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                CurrentPosition = TimeSpan.FromMilliseconds(e.Time);
                CurrentPositionString = FormatDuration(CurrentPosition);
            });
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = false;
                CurrentPosition = TimeSpan.Zero;
                CurrentPositionString = "0:00";
                if (CurrentAudio != null)
                {
                    CurrentAudio.IsPlaying = false;
                }
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            });
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = false;
                if (CurrentAudio != null)
                {
                    CurrentAudio.IsPlaying = false;
                }
                PlaybackStateChanged?.Invoke();
            });
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = true;
                if (CurrentAudio != null)
                {
                    CurrentAudio.IsPlaying = true;
                }
                PlaybackStateChanged?.Invoke();
            });
        }

        private async void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                IsPlaying = false;
                CurrentPosition = TotalDuration;
                CurrentPositionString = TotalDurationString;
                if (CurrentAudio != null)
                {
                    CurrentAudio.IsPlaying = false;
                }
                PlaybackStateChanged?.Invoke();

                if (RepeatMode == RepeatMode.One)
                {
                    await PlayAudioAsync(CurrentAudio);
                }
                else if (CanSkipNext())
                {
                    await SkipNextAsync();
                }
                else if (RepeatMode == RepeatMode.All)
                {
                    // Restart from beginning
                    var firstAudio = GetPlaylist().FirstOrDefault();
                    if (firstAudio != null)
                    {
                        await PlayAudioAsync(firstAudio);
                    }
                }
                else
                {
                    StopPlaybackInternal();
                }
                UpdateCommandStates();
            });
        }

        public async Task LoadAudioFilesAsync(StorageFolder folder)
        {
            if (folder == null)
            {
                if (!this.IsPlaying)
                {
                    AudioFiles.Clear();
                    FilteredAudioFiles.Clear();
                    FavoriteAudioFiles.Clear();
                    CurrentAudio = null;
                    ResetPlaybackState();
                    UpdateShufflePlaylist();
                    PlaybackStateChanged?.Invoke();
                    UpdateCommandStates();
                }
                return;
            }
            
            var audioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".wav", ".aac", ".flac", ".wma", ".ogg", ".m4a"
            };
            LocalAudioModel previouslyPlayingAudio = null;
            if (this.IsPlaying && this.CurrentAudio != null)
            {
                previouslyPlayingAudio = this.CurrentAudio;
            }
            AudioFiles.Clear();
            FilteredAudioFiles.Clear();

            if (previouslyPlayingAudio == null)
            {
                CurrentAudio = null;
                ResetPlaybackState();
            }

            try
            {
                var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, audioExtensions.ToList())
                {
                    FolderDepth = FolderDepth.Deep
                };
                var queryResult = folder.CreateItemQueryWithOptions(queryOptions);
                var items = await queryResult.GetItemsAsync();
                bool currentPlayingAudioStillExistsInNewFolder = false;

                foreach (var item in items)
                {
                    if (item is StorageFile file && audioExtensions.Contains(file.FileType.ToLowerInvariant()))
                    {
                        try
                        {
                            var audioModel = await LocalAudioModel.FromStorageFileAsync(file);
                            AudioFiles.Add(audioModel);
                            FilteredAudioFiles.Add(audioModel);
                            if (previouslyPlayingAudio != null && audioModel.FilePath.Equals(previouslyPlayingAudio.FilePath, StringComparison.OrdinalIgnoreCase))
                            {
                                currentPlayingAudioStillExistsInNewFolder = true;
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating LocalAudioModel for {file.Name}: {ex.Message}");
                        }
                    }
                }
                if (previouslyPlayingAudio == null && !AudioFiles.Any())
                {
                    CurrentAudio = null;
                    ResetPlaybackState();
                }
                UpdateShufflePlaylist();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading audio files from {folder.Path}: {ex.Message}");
                if (previouslyPlayingAudio == null)
                {
                    CurrentAudio = null;
                    ResetPlaybackState();
                }
            }
            finally
            {
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            }
        }

        [RelayCommand(CanExecute = nameof(CanPlayAudio))]
        public async Task PlayAudioAsync(LocalAudioModel audio)
        {
            if (audio?.File == null) return;

            StopPlaybackInternal();

            try
            {
                if (CurrentAudio != null)
                {
                    CurrentAudio.IsPlaying = false;
                    CurrentAudio.IsSelected = false;
                }

                CurrentAudio = audio;
                CurrentAudio.IsSelected = true;
                CurrentAudio.IsPlaying = true;
                NowPlayingTitle = audio.DisplayTitle;
                NowPlayingArtist = audio.DisplayArtist;
                NowPlayingAlbum = audio.DisplayAlbum;

                IsMediaPlayerElementVisible = false; // Audio only

                _currentMediaTrack = new Media(_libVLC, audio.File.Path, FromType.FromPath);
                _mediaPlayer.Media = _currentMediaTrack;
                bool success = _mediaPlayer.Play();

                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"MediaPlayer.Play() failed for {audio.File.Path}");
                    ResetCurrentAudio();
                    NowPlayingTitle = "Lỗi khi phát file";
                    IsPlaying = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing audio file {audio.File.Path}: {ex.Message}");
                ResetCurrentAudio();
                NowPlayingTitle = "Lỗi khi chuẩn bị file";
                IsPlaying = false;
            }
            finally
            {
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            }
        }

        private bool CanPlayAudio(LocalAudioModel audio) => audio?.File != null;

        [RelayCommand(CanExecute = nameof(CanTogglePlayPause))]
        public void TogglePlayPause()
        {
            if (_mediaPlayer?.Media == null) return;

            if (_mediaPlayer.State == VLCState.Playing)
            {
                _mediaPlayer.Pause();
            }
            else
            {
                _mediaPlayer.Play();
            }
        }

        private bool CanTogglePlayPause() => _mediaPlayer?.Media != null && CurrentAudio != null;

        [RelayCommand(CanExecute = nameof(CanStopPlayback))]
        public void StopPlayback()
        {
            StopPlaybackInternal();
            PlaybackStateChanged?.Invoke();
            UpdateCommandStates();
        }

        private bool CanStopPlayback() => _mediaPlayer?.Media != null && CurrentAudio != null;

        private void StopPlaybackInternal()
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying || _mediaPlayer.State == VLCState.Paused)
                {
                    _mediaPlayer.Stop();
                }
            }

            _currentMediaTrack?.Dispose();
            _currentMediaTrack = null;

            if (CurrentAudio != null)
            {
                CurrentAudio.IsPlaying = false;
                CurrentAudio.IsSelected = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSeekPosition))]
        public void Seek(TimeSpan position)
        {
            if (_mediaPlayer?.Media != null && _mediaPlayer.IsSeekable)
            {
                _mediaPlayer.Time = (long)position.TotalMilliseconds;
            }
        }

        private bool CanSeekPosition(TimeSpan position) => _mediaPlayer?.Media != null && _mediaPlayer.IsSeekable && CurrentAudio != null;

        partial void OnVolumeChanged(int value)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = Math.Clamp(value, 0, 100);
            }
        }

        private List<LocalAudioModel> GetPlaylist()
        {
            return ShuffleMode == ShuffleMode.On ? _shuffledPlaylist ?? AudioFiles.ToList() : AudioFiles.ToList();
        }

        private int GetCurrentAudioIndex()
        {
            if (CurrentAudio == null) return -1;
            var playlist = GetPlaylist();
            return playlist.FindIndex(a => a.File.Path.Equals(CurrentAudio.File.Path, StringComparison.OrdinalIgnoreCase));
        }

        [RelayCommand(CanExecute = nameof(CanSkipPrevious))]
        public async Task SkipPreviousAsync()
        {
            var playlist = GetPlaylist();
            int currentIndex = GetCurrentAudioIndex();

            if (currentIndex > 0)
            {
                await PlayAudioAsync(playlist[currentIndex - 1]);
            }
            else if (RepeatMode == RepeatMode.All)
            {
                await PlayAudioAsync(playlist.LastOrDefault());
            }
        }

        private bool CanSkipPrevious()
        {
            if (CurrentAudio == null || AudioFiles.Count <= 1) return false;
            return GetCurrentAudioIndex() > 0 || RepeatMode == RepeatMode.All;
        }

        [RelayCommand(CanExecute = nameof(CanSkipNext))]
        public async Task SkipNextAsync()
        {
            var playlist = GetPlaylist();
            int currentIndex = GetCurrentAudioIndex();

            if (currentIndex >= 0 && currentIndex < playlist.Count - 1)
            {
                await PlayAudioAsync(playlist[currentIndex + 1]);
            }
            else if (RepeatMode == RepeatMode.All)
            {
                await PlayAudioAsync(playlist.FirstOrDefault());
            }
        }

        private bool CanSkipNext()
        {
            if (CurrentAudio == null || AudioFiles.Count <= 1) return false;
            int currentIndex = GetCurrentAudioIndex();
            return (currentIndex >= 0 && currentIndex < AudioFiles.Count - 1) || RepeatMode == RepeatMode.All;
        }

        [RelayCommand]
        public void ToggleRepeatMode()
        {
            RepeatMode = RepeatMode switch
            {
                RepeatMode.None => RepeatMode.All,
                RepeatMode.All => RepeatMode.One,
                RepeatMode.One => RepeatMode.None,
                _ => RepeatMode.None
            };
            OnPropertyChanged(nameof(RepeatGlyph));
        }

        [RelayCommand]
        public void ToggleShuffleMode()
        {
            ShuffleMode = ShuffleMode == ShuffleMode.Off ? ShuffleMode.On : ShuffleMode.Off;
            UpdateShufflePlaylist();
            OnPropertyChanged(nameof(ShuffleGlyph));
        }

        private void UpdateShufflePlaylist()
        {
            if (ShuffleMode == ShuffleMode.On)
            {
                _shuffledPlaylist = AudioFiles.OrderBy(x => _random.Next()).ToList();
            }
            else
            {
                _shuffledPlaylist = null;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterAudioFiles();
        }

        private void FilterAudioFiles()
        {
            FilteredAudioFiles.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var audio in AudioFiles)
                {
                    FilteredAudioFiles.Add(audio);
                }
            }
            else
            {
                var searchLower = SearchText.ToLower();
                foreach (var audio in AudioFiles)
                {
                    if (audio.SongTitle?.ToLower().Contains(searchLower) == true ||
                        audio.Artist?.ToLower().Contains(searchLower) == true ||
                        audio.Album?.ToLower().Contains(searchLower) == true ||
                        audio.Genre?.ToLower().Contains(searchLower) == true)
                    {
                        FilteredAudioFiles.Add(audio);
                    }
                }
            }
        }

        [RelayCommand]
        public void ClearSearch()
        {
            SearchText = "";
        }

        [RelayCommand]
        public void ToggleFavorite(LocalAudioModel audio)
        {
            if (audio == null) return;

            audio.ToggleFavorite();
            UpdateFavoritesList();
        }

        private void UpdateFavoritesList()
        {
            FavoriteAudioFiles.Clear();
            foreach (var audio in AudioFiles.Where(a => a.IsFavorite))
            {
                FavoriteAudioFiles.Add(audio);
            }
        }
        public void ResetPlaybackState()
        {
            NowPlayingTitle = "Không có file nào đang phát";
            NowPlayingArtist = "";
            NowPlayingAlbum = "";
            TotalDuration = TimeSpan.Zero;
            CurrentPosition = TimeSpan.Zero;
            TotalDurationString = "0:00";
            CurrentPositionString = "0:00";
            IsPlaying = false;
            IsMediaPlayerElementVisible = false;
        }

        private void ResetCurrentAudio()
        {
            if (CurrentAudio != null)
            {
                CurrentAudio.IsPlaying = false;
                CurrentAudio.IsSelected = false;
            }
            CurrentAudio = null;
            NowPlayingArtist = "";
            NowPlayingAlbum = "";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return duration.ToString(@"h\:mm\:ss");
            }
            return duration.ToString(@"m\:ss");
        }

        private void UpdateCommandStates()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                PlayAudioCommand.NotifyCanExecuteChanged();
                TogglePlayPauseCommand.NotifyCanExecuteChanged();
                StopPlaybackCommand.NotifyCanExecuteChanged();
                SeekCommand.NotifyCanExecuteChanged();
                SkipPreviousCommand.NotifyCanExecuteChanged();
                SkipNextCommand.NotifyCanExecuteChanged();
            });
        }

        public void Cleanup()
        {
            StopPlaybackInternal();

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

            _currentMediaTrack?.Dispose();
            _currentMediaTrack = null;

            _libVLC?.Dispose();
            _libVLC = null;

            AudioFiles.Clear();
            FilteredAudioFiles.Clear();
            FavoriteAudioFiles.Clear();
        }
    }
}