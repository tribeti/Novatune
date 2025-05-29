using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace App2.Models
{
    public class LocalModel : INotifyPropertyChanged
    {
        private string _songTitle;
        private string _artist;
        private string _album;
        private string _genre;
        private uint _year;
        private uint _trackNumber;
        private TimeSpan _duration;
        private string _durationString;
        private StorageItemThumbnail _thumbnail;
        private StorageFile _file;
        private string _filePath;
        private ulong _fileSize;
        private bool _isPlaying;
        private bool _isFavorite;
        private bool _isSelected;

        public string SongTitle
        {
            get => _songTitle;
            set => SetProperty(ref _songTitle, value);
        }

        public string Artist
        {
            get => _artist;
            set => SetProperty(ref _artist, value);
        }

        public string Album
        {
            get => _album;
            set => SetProperty(ref _album, value);
        }

        public string Genre
        {
            get => _genre;
            set => SetProperty(ref _genre, value);
        }

        public uint Year
        {
            get => _year;
            set => SetProperty(ref _year, value);
        }

        public uint TrackNumber
        {
            get => _trackNumber;
            set => SetProperty(ref _trackNumber, value);
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (SetProperty(ref _duration, value))
                {
                    DurationString = FormatDuration(value);
                }
            }
        }

        public string DurationString
        {
            get => _durationString;
            private set => SetProperty(ref _durationString, value);
        }

        public StorageItemThumbnail Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        public StorageFile File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public ulong FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        // Computed properties
        public string DisplayTitle => !string.IsNullOrWhiteSpace(SongTitle) ? SongTitle : "Unknown Title";
        public string DisplayArtist => !string.IsNullOrWhiteSpace(Artist) ? Artist : "Unknown Artist";
        public string DisplayAlbum => !string.IsNullOrWhiteSpace(Album) ? Album : "Unknown Album";
        public string FileSizeString => FormatFileSize(FileSize);

        private LocalModel() { }

        public static async Task<LocalModel> FromStorageFileAsync(StorageFile file)
        {
            try
            {
                var musicProperties = await file.Properties.GetMusicPropertiesAsync();
                var basicProperties = await file.GetBasicPropertiesAsync();
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200);

                var model = new LocalModel
                {
                    SongTitle = string.IsNullOrWhiteSpace(musicProperties.Title) ? file.DisplayName : musicProperties.Title,
                    Artist = musicProperties.Artist ?? string.Empty,
                    Album = musicProperties.Album ?? string.Empty,
                    Genre = string.Join(", ", musicProperties.Genre),
                    Year = musicProperties.Year,
                    TrackNumber = musicProperties.TrackNumber,
                    Duration = musicProperties.Duration,
                    Thumbnail = thumbnail,
                    File = file,
                    FilePath = file.Path,
                    FileSize = basicProperties.Size,
                    IsPlaying = false,
                    IsFavorite = false,
                    IsSelected = false
                };

                model.DurationString = FormatDuration(model.Duration);
                return model;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create LocalAudioModel from file: {file?.Name}", ex);
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return duration.ToString(@"h\:mm\:ss");
            }
            return duration.ToString(@"m\:ss");
        }

        private static string FormatFileSize(ulong bytes)
        {
            const ulong KB = 1024;
            const ulong MB = KB * 1024;
            const ulong GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes / (double)GB:F2} GB";
            if (bytes >= MB)
                return $"{bytes / (double)MB:F2} MB";
            if (bytes >= KB)
                return $"{bytes / (double)KB:F2} KB";
            return $"{bytes} bytes";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ToggleFavorite()
        {
            IsFavorite = !IsFavorite;
        }

        public void SetPlayingState(bool isPlaying)
        {
            IsPlaying = isPlaying;
        }

        public override string ToString()
        {
            return $"{DisplayArtist} - {DisplayTitle}";
        }
    }
}