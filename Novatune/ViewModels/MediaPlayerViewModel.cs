using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Novatune.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using Windows.Storage;
using Windows.Storage.Search;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using Novatune.Enums;

namespace Novatune.ViewModels
{  
    public partial class MediaPlayerViewModel : ObservableObject
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private Media? _currentMediaTrack;
        private readonly DispatcherQueue? _dispatcherQueue;
        private static bool _isLibVLCSharpCoreInitialized = false;
        private List<LocalModel> _shuffledPlaylist;
        private Random _random = new ();
        private YoutubeClient _youtubeClient;

        public ObservableCollection<LocalModel> AudioFiles { get; } = new ();
        public ObservableCollection<LocalModel> FilteredAudioFiles { get; } = new ();
        public ObservableCollection<LocalModel> FavoriteAudioFiles { get; } = new ();
        public ObservableCollection<OnlineModel>? OnlineAudioTracks { get; } = new ();


        [ObservableProperty]
        public partial LocalModel? CurrentAudio { get; set; }

        [ObservableProperty]
        public partial OnlineModel? CurrentOnlineAudio { get; set; }

        [ObservableProperty]
        public partial string NowPlayingTitle { get; set; } = "Không có file nào đang phát";

        [ObservableProperty]
        public partial string NowPlayingArtist { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string NowPlayingAlbum { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor (nameof (PlayPauseGlyph))]
        public partial bool IsPlaying { get; set; }

        [ObservableProperty]
        public partial bool IsMediaPlayerElementVisible { get; set; }

        [ObservableProperty]
        public partial TimeSpan CurrentPosition { get; set; }

        [ObservableProperty]
        public partial TimeSpan TotalDuration { get; set; }

        [ObservableProperty]
        public partial string CurrentPositionString { get; set; } = "0:00";

        [ObservableProperty]
        public partial string TotalDurationString { get; set; } = "0:00";

        [ObservableProperty]
        public partial MediaEnums.RepeatMode RepeatMode { get; set; } = MediaEnums.RepeatMode.None;

        [ObservableProperty]
        public partial MediaEnums.ShuffleMode ShuffleMode { get; set; } = MediaEnums.ShuffleMode.Off;

        [ObservableProperty]
        public partial int Volume { get; set; } = 300;

        [ObservableProperty]
        public partial string? SearchText { get; set; } = string.Empty;

        public string PlayPauseGlyph => IsPlaying ? "\uE769" : "\uE768";
        public string RepeatGlyph => RepeatMode switch
        {
            MediaEnums.RepeatMode.None => "\uF5E7",
            MediaEnums.RepeatMode.One => "\uE8ED",
            MediaEnums.RepeatMode.All => "\uE8EE",
            _ => "\uE8EE"
        };
        public string ShuffleGlyph => ShuffleMode == MediaEnums.ShuffleMode.On ? "\uE8B1" : "\uE8B1";

        public event Action? PlaybackStateChanged;
        public LibVLCSharp.Shared.MediaPlayer? PlayerInstance => _mediaPlayer;

        public MediaPlayerViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Cannot get DispatcherQueue for current thread.");
            _youtubeClient = new YoutubeClient();

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
            _mediaPlayer.Volume = Math.Clamp(Volume, 0, 100);
        }

        private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                NowPlayingTitle = "Lỗi trình phát";
                NowPlayingArtist = "";
                NowPlayingAlbum = "";
                IsPlaying = false;
                CurrentPosition = TimeSpan.Zero;
                if (CurrentAudio != null) CurrentAudio.IsPlaying = false;
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            });
        }

        private void MediaPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                TotalDuration = TimeSpan.FromMilliseconds(e.Length);
                TotalDurationString = FormatDuration(TotalDuration);
            });
        }

        private void MediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                CurrentPosition = TimeSpan.FromMilliseconds(e.Time);
                CurrentPositionString = FormatDuration(CurrentPosition);
            });
        }

        private void MediaPlayer_Stopped(object? sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = false;
                CurrentPosition = TimeSpan.Zero;
                CurrentPositionString = "0:00";
                if (CurrentAudio != null && CurrentOnlineAudio == null) CurrentAudio.IsPlaying = false;
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            });
        }

        private void MediaPlayer_Paused(object? sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = false;
                if (CurrentAudio != null && CurrentOnlineAudio == null) CurrentAudio.IsPlaying = false;
                PlaybackStateChanged?.Invoke();
            });
        }

        private void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = true;
                if (CurrentAudio != null && CurrentOnlineAudio == null) CurrentAudio.IsPlaying = true;
                PlaybackStateChanged?.Invoke();
            });
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                IsPlaying = false;
                CurrentPosition = TotalDuration;
                CurrentPositionString = TotalDurationString;

                bool wasPlayingLocal = CurrentAudio != null && CurrentOnlineAudio == null;
                bool wasPlayingOnline = CurrentOnlineAudio != null;

                if (wasPlayingLocal && CurrentAudio != null) CurrentAudio.IsPlaying = false;

                PlaybackStateChanged?.Invoke();
                bool playedNext = false;

                if (RepeatMode == MediaEnums.RepeatMode.One)
                {
                    if (wasPlayingOnline && CurrentOnlineAudio != null && !string.IsNullOrEmpty(CurrentOnlineAudio.VideoId))
                    {
                        try
                        {
                            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(CurrentOnlineAudio.VideoId);
                            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().TryGetWithHighestBitrate();
                            if (audioStreamInfo != null)
                            {
                                CurrentOnlineAudio.StreamUrl = audioStreamInfo.Url;
                                await PlayOnlineAudioAsync(CurrentOnlineAudio);
                                playedNext = true;
                            }
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error re-fetching stream for repeat: {ex.Message}"); }
                    }
                    else if (wasPlayingLocal && CurrentAudio != null)
                    {
                        await PlayAudioAsync(CurrentAudio);
                        playedNext = true;
                    }
                }

                if (!playedNext)
                {
                    if (wasPlayingOnline)
                    {
                        int currentIndexInOnlinePlaylist = OnlineAudioTracks.IndexOf(CurrentOnlineAudio);
                        if (currentIndexInOnlinePlaylist >= 0 && currentIndexInOnlinePlaylist < OnlineAudioTracks.Count - 1)
                        {
                            await PlayOnlineAudioAsync(OnlineAudioTracks[currentIndexInOnlinePlaylist + 1]);
                            playedNext = true;
                        }
                        else if (RepeatMode == MediaEnums.RepeatMode.All && OnlineAudioTracks.Any())
                        {
                            await PlayOnlineAudioAsync(OnlineAudioTracks.First());
                            playedNext = true;
                        }
                    }

                    if (!playedNext && wasPlayingLocal) // Hoặc nếu muốn fallback từ online sang local
                    {
                        if (CanSkipNext())
                        {
                            await SkipNextAsync();
                            playedNext = true;
                        }
                        else if (RepeatMode == MediaEnums.RepeatMode.All && AudioFiles.Any())
                        {
                            var firstAudio = GetPlaylist().FirstOrDefault();
                            if (firstAudio != null)
                            {
                                await PlayAudioAsync(firstAudio);
                                playedNext = true;
                            }
                        }
                    }
                }

                if (!playedNext)
                {
                    StopPlaybackInternal();
                    if (CurrentAudio is not null) ResetCurrentAudio();
                    if (CurrentOnlineAudio != null)
                    {
                        CurrentOnlineAudio = null;
                        if (CurrentAudio is null) ResetPlaybackState();
                    }
                    else if (CurrentAudio is null)
                    {
                        ResetPlaybackState();
                    }
                }
                UpdateCommandStates();
            });
        }

        public async Task LoadOnlinePlaylistAsync(string playlistUrlOrId)
        {
            if (string.IsNullOrWhiteSpace(playlistUrlOrId)) return;

            StopPlaybackInternal();
            AudioFiles.Clear();
            FilteredAudioFiles.Clear();
            FavoriteAudioFiles.Clear();
            CurrentAudio = null;
            OnlineAudioTracks.Clear();
            ResetPlaybackState();

            try
            {
                await foreach (var video in _youtubeClient.Playlists.GetVideosAsync(playlistUrlOrId))
                {
                    var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
                    var audioStreamInfo = streamManifest.GetAudioOnlyStreams().TryGetWithHighestBitrate();
                    if (audioStreamInfo != null)
                    {
                        OnlineAudioTracks.Add(new OnlineModel
                        {
                            Title = video.Title,
                            Author = video.Author.ChannelTitle,
                            DurationTimeSpan = video.Duration,
                            StreamUrl = audioStreamInfo.Url,
                            ThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url,
                            VideoId = video.Id.Value
                        });
                    }
                }
                if (OnlineAudioTracks.Any())
                {
                    await PlayOnlineAudioAsync(OnlineAudioTracks.First());
                }
                else
                {
                    NowPlayingTitle = "Online playlist error";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading online playlist: {ex.Message}");
                NowPlayingTitle = "Lỗi tải playlist online.";
            }
            finally
            {
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            }
        }

        public async Task LoadAudioFilesAsync(StorageFolder? folder)
        {
            if (folder == null)
            {
                if (!this.IsPlaying && CurrentOnlineAudio == null)
                {
                    AudioFiles.Clear();
                    FilteredAudioFiles.Clear();
                    FavoriteAudioFiles.Clear();
                    CurrentAudio = null;
                    ResetPlaybackState();
                    UpdateShufflePlaylist();
                }
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
                return;
            }

            var audioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".aac", ".flac", ".wma", ".ogg", ".m4a" };
            LocalModel? previouslyPlayingLocalAudio = null;
            if (this.IsPlaying && this.CurrentAudio != null && this.CurrentOnlineAudio == null)
            {
                previouslyPlayingLocalAudio = this.CurrentAudio;
            }

            AudioFiles.Clear();
            FilteredAudioFiles.Clear();

            if (CurrentOnlineAudio == null && previouslyPlayingLocalAudio == null)
            {
                CurrentAudio = null;
                ResetPlaybackState();
            }

            try
            {
                var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, audioExtensions.ToList()) { FolderDepth = FolderDepth.Deep };
                var queryResult = folder.CreateItemQueryWithOptions(queryOptions);
                var items = await queryResult.GetItemsAsync();

                foreach (var item in items)
                {
                    if (item is StorageFile file && audioExtensions.Contains(file.FileType.ToLowerInvariant()))
                    {
                        try
                        {
                            var audioModel = await LocalModel.FromStorageFileAsync(file);
                            AudioFiles.Add(audioModel);
                            FilteredAudioFiles.Add(audioModel);
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error creating LocalAudioModel for {file.Name}: {ex.Message}"); }
                    }
                }

                if (CurrentOnlineAudio == null && previouslyPlayingLocalAudio == null && !AudioFiles.Any())
                {
                    CurrentAudio = null;
                    ResetPlaybackState();
                }
                UpdateShufflePlaylist();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading audio files from {folder.Path}: {ex.Message}");
                if (CurrentOnlineAudio == null && previouslyPlayingLocalAudio == null)
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
        public async Task PlayAudioAsync(LocalModel audio)
        {
            if (audio?.File == null) return;

            StopPlaybackInternal();
            CurrentOnlineAudio = null;

            try
            {
                if (CurrentAudio != null && CurrentAudio != audio)
                {
                    CurrentAudio.IsPlaying = false;
                    CurrentAudio.IsSelected = false;
                }

                CurrentAudio = audio;
                CurrentAudio.IsSelected = true;

                NowPlayingTitle = audio.DisplayTitle;
                NowPlayingArtist = audio.DisplayArtist;
                NowPlayingAlbum = audio.DisplayAlbum;
                IsMediaPlayerElementVisible = false;

                _currentMediaTrack = new Media(_libVLC, audio.File.Path, FromType.FromPath);
                _mediaPlayer.Media = _currentMediaTrack;
                _mediaPlayer.Volume = Math.Clamp(Volume, 0, 100);
                bool success = _mediaPlayer.Play();

                if (!success)
                {
                    ResetCurrentAudio();
                    NowPlayingTitle = "Lỗi khi phát file";
                    IsPlaying = false;
                }
            }
            catch (Exception ex)
            {
                ResetCurrentAudio();
                NowPlayingTitle = "Lỗi khi chuẩn bị file";
                IsPlaying = false;
                System.Diagnostics.Debug.WriteLine($"Error playing audio file {audio.File.Path}: {ex.Message}");
            }
            finally
            {
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            }
        }
        private bool CanPlayAudio(LocalModel audio) => audio?.File != null;

        [RelayCommand(CanExecute = nameof(CanPlayOnlineAudio))]
        public async Task PlayOnlineAudioAsync(OnlineModel onlineTrack)
        {
            if (onlineTrack == null || string.IsNullOrEmpty(onlineTrack.StreamUrl)) return;

            StopPlaybackInternal();
            if (CurrentAudio != null)
            {
                CurrentAudio.IsPlaying = false;
                CurrentAudio.IsSelected = false;
            }
            CurrentAudio = null;
            CurrentOnlineAudio = onlineTrack;

            try
            {
                NowPlayingTitle = onlineTrack.DisplayTitle;
                NowPlayingArtist = onlineTrack.DisplayArtist;
                NowPlayingAlbum = "YouTube";
                IsMediaPlayerElementVisible = false;

                CurrentPosition = TimeSpan.Zero;
                TotalDuration = onlineTrack.DurationTimeSpan ?? TimeSpan.Zero;
                CurrentPositionString = FormatDuration(CurrentPosition);
                TotalDurationString = FormatDuration(TotalDuration);

                _currentMediaTrack = new Media(_libVLC, onlineTrack.StreamUrl, FromType.FromLocation);
                _mediaPlayer.Media = _currentMediaTrack;
                _mediaPlayer.Volume = Math.Clamp(Volume, 0, 100);
                bool success = _mediaPlayer.Play();

                if (!success)
                {
                    NowPlayingTitle = "Lỗi khi phát online";
                    IsPlaying = false;
                    CurrentOnlineAudio = null;
                }
            }
            catch (Exception ex)
            {
                NowPlayingTitle = "Lỗi khi chuẩn bị file online";
                IsPlaying = false;
                CurrentOnlineAudio = null;
                System.Diagnostics.Debug.WriteLine($"Error playing online audio {onlineTrack.Title}: {ex.Message}");
            }
            finally
            {
                PlaybackStateChanged?.Invoke();
                UpdateCommandStates();
            }
        }
        private bool CanPlayOnlineAudio(OnlineModel onlineTrack) => onlineTrack != null && !string.IsNullOrEmpty(onlineTrack.StreamUrl);


        [RelayCommand(CanExecute = nameof(CanTogglePlayPause))]
        public void TogglePlayPause()
        {
            if (_mediaPlayer?.Media == null) return;
            if (_mediaPlayer.State == VLCState.Playing) _mediaPlayer.Pause();
            else _mediaPlayer.Play();
        }
        private bool CanTogglePlayPause() => _mediaPlayer?.Media != null && (CurrentAudio != null || CurrentOnlineAudio != null);

        [RelayCommand(CanExecute = nameof(CanStopPlayback))]
        public void StopPlayback()
        {
            StopPlaybackInternal();
            if (CurrentAudio != null) ResetCurrentAudio();
            if (CurrentOnlineAudio != null)
            {
                CurrentOnlineAudio = null;
                if (CurrentAudio == null) ResetPlaybackState();
            }
            else if (CurrentAudio == null)
            {
                ResetPlaybackState();
            }
            PlaybackStateChanged?.Invoke();
            UpdateCommandStates();
        }
        private bool CanStopPlayback() => _mediaPlayer?.Media != null && (CurrentAudio != null || CurrentOnlineAudio != null);

        private void StopPlaybackInternal()
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying || _mediaPlayer.State == VLCState.Paused || _mediaPlayer.State == VLCState.Opening)
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
        private bool CanSeekPosition(TimeSpan position) => _mediaPlayer?.Media != null && _mediaPlayer.IsSeekable && (CurrentAudio != null || CurrentOnlineAudio != null);

        partial void OnVolumeChanged(int value)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = Math.Clamp(value, 0, 100);
            }
        }

        private List<LocalModel> GetPlaylist()
        {
            return ShuffleMode == MediaEnums.ShuffleMode.On ? _shuffledPlaylist ?? AudioFiles.ToList() : AudioFiles.ToList();
        }

        private int GetCurrentAudioIndex()
        {
            if (CurrentAudio == null || CurrentOnlineAudio != null) return -1;
            var playlist = GetPlaylist();
            return playlist.FindIndex(a => a.File.Path.Equals(CurrentAudio.File.Path, StringComparison.OrdinalIgnoreCase));
        }

        [RelayCommand(CanExecute = nameof(CanSkipPrevious))]
        public async Task SkipPreviousAsync()
        {
            if (CurrentOnlineAudio != null)
            {
                int currentIndexInOnlinePlaylist = OnlineAudioTracks.IndexOf(CurrentOnlineAudio);
                if (currentIndexInOnlinePlaylist > 0)
                {
                    await PlayOnlineAudioAsync(OnlineAudioTracks[currentIndexInOnlinePlaylist - 1]);
                }
                else if (RepeatMode == MediaEnums.RepeatMode.All && OnlineAudioTracks.Any())
                {
                    await PlayOnlineAudioAsync(OnlineAudioTracks.Last());
                }
                return;
            }

            var playlist = GetPlaylist();
            int currentIndex = GetCurrentAudioIndex();
            if (currentIndex > 0) await PlayAudioAsync(playlist[currentIndex - 1]);
            else if (RepeatMode == MediaEnums.RepeatMode.All && playlist.Any()) await PlayAudioAsync(playlist.Last());
        }
        private bool CanSkipPrevious()
        {
            if (CurrentOnlineAudio != null)
            {
                int currentIndexInOnlinePlaylist = OnlineAudioTracks.IndexOf(CurrentOnlineAudio);
                return currentIndexInOnlinePlaylist > 0 || (RepeatMode == MediaEnums.RepeatMode.All && OnlineAudioTracks.Count > 1);
            }
            if (CurrentAudio == null || AudioFiles.Count <= 1) return false;
            return GetCurrentAudioIndex() > 0 || (RepeatMode == MediaEnums.RepeatMode.All && AudioFiles.Count > 1);
        }

        [RelayCommand(CanExecute = nameof(CanSkipNext))]
        public async Task SkipNextAsync()
        {
            if (CurrentOnlineAudio != null)
            {
                int currentIndexInOnlinePlaylist = OnlineAudioTracks.IndexOf(CurrentOnlineAudio);
                if (currentIndexInOnlinePlaylist >= 0 && currentIndexInOnlinePlaylist < OnlineAudioTracks.Count - 1)
                {
                    await PlayOnlineAudioAsync(OnlineAudioTracks[currentIndexInOnlinePlaylist + 1]);
                }
                else if (RepeatMode == MediaEnums.RepeatMode.All && OnlineAudioTracks.Any())
                {
                    await PlayOnlineAudioAsync(OnlineAudioTracks.First());
                }
                return;
            }

            var playlist = GetPlaylist();
            int currentIndex = GetCurrentAudioIndex();
            if (currentIndex >= 0 && currentIndex < playlist.Count - 1) await PlayAudioAsync(playlist[currentIndex + 1]);
            else if (RepeatMode == MediaEnums.RepeatMode.All && playlist.Any()) await PlayAudioAsync(playlist.First());
        }
        private bool CanSkipNext()
        {
            if (CurrentOnlineAudio != null)
            {
                int currentIndexInOnlinePlaylist = OnlineAudioTracks.IndexOf(CurrentOnlineAudio);
                return (currentIndexInOnlinePlaylist >= 0 && currentIndexInOnlinePlaylist < OnlineAudioTracks.Count - 1) || (RepeatMode == MediaEnums.RepeatMode.All && OnlineAudioTracks.Count > 1);
            }
            if (CurrentAudio == null || AudioFiles.Count <= 1) return false;
            int currentIndex = GetCurrentAudioIndex();
            return (currentIndex >= 0 && currentIndex < AudioFiles.Count - 1) || (RepeatMode == MediaEnums.RepeatMode.All && AudioFiles.Count > 1);
        }

        [RelayCommand]
        public void ToggleRepeatMode()
        {
            RepeatMode = RepeatMode switch { MediaEnums.RepeatMode.None => MediaEnums.RepeatMode.All, MediaEnums.RepeatMode.All => MediaEnums.RepeatMode.One, MediaEnums.RepeatMode.One => MediaEnums.RepeatMode.None, _ => MediaEnums.RepeatMode.None };
            OnPropertyChanged(nameof(RepeatGlyph));
        }

        [RelayCommand]
        public void ToggleShuffleMode()
        {
            ShuffleMode = ShuffleMode == MediaEnums.ShuffleMode.Off ? MediaEnums.ShuffleMode.On : MediaEnums.ShuffleMode.Off;
            UpdateShufflePlaylist();
            OnPropertyChanged(nameof(ShuffleGlyph));
        }

        private void UpdateShufflePlaylist()
        {
            if (ShuffleMode == MediaEnums.ShuffleMode.On) _shuffledPlaylist = AudioFiles.OrderBy(x => _random.Next()).ToList();
            else _shuffledPlaylist = null;
        }

        partial void OnSearchTextChanged(string value) { FilterAudioFiles(); }

        private void FilterAudioFiles()
        {
            FilteredAudioFiles.Clear();
            if (string.IsNullOrWhiteSpace(SearchText)) foreach (var audio in AudioFiles) FilteredAudioFiles.Add(audio);
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
        public void ClearSearch() { SearchText = ""; }

        [RelayCommand]
        public void ToggleFavorite(LocalModel audio)
        {
            if (audio == null) return;
            audio.ToggleFavorite();
            UpdateFavoritesList();
        }

        private void UpdateFavoritesList()
        {
            FavoriteAudioFiles.Clear();
            foreach (var audio in AudioFiles.Where(a => a.IsFavorite)) FavoriteAudioFiles.Add(audio);
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
            if (CurrentOnlineAudio == null)
            {
                NowPlayingArtist = "";
                NowPlayingAlbum = "";
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1) return duration.ToString(@"h\:mm\:ss");
            return duration.ToString(@"m\:ss");
        }

        private void UpdateCommandStates()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                PlayAudioCommand.NotifyCanExecuteChanged();
                PlayOnlineAudioCommand.NotifyCanExecuteChanged();
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
            OnlineAudioTracks.Clear();
            CurrentAudio = null;
            CurrentOnlineAudio = null;
        }
    }
}