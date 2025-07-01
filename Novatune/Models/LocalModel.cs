using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Novatune.Models
{
    public partial class LocalModel : ObservableObject, INotifyPropertyChanged
    {
        [ObservableProperty]
        public partial string SongTitle { get; set; }

        [ObservableProperty]
        public partial string Artist { get; set; }

        [ObservableProperty]
        public partial string Album { get; set; }

        [ObservableProperty]
        public partial string Genre { get; set; }
        
        [ObservableProperty]
        public partial uint Year { get; set; }
        
        [ObservableProperty]
        public partial uint TrackNumber { get; set; }
               
        [ObservableProperty]
        public partial StorageItemThumbnail Thumbnail { get; set; }

        [ObservableProperty]
        public partial StorageFile File { get; set; }

        [ObservableProperty]
        public partial string FilePath { get; set; }        

        [ObservableProperty]
        public partial ulong FileSize { get; set; }

        [ObservableProperty]
        public partial bool IsPlaying { get; set; }

        [ObservableProperty]
        public partial bool IsSelected { get; set; }

        [ObservableProperty]
        public partial bool IsFavorite { get; set; }

        private TimeSpan _duration;
        private string _durationString;

        public string DurationString
        {
            get => _durationString;
            private set => SetProperty (ref _durationString, value);
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

        public string DisplayTitle => !string.IsNullOrWhiteSpace(SongTitle) ? SongTitle : "Unknown Title";
        public string DisplayArtist => !string.IsNullOrWhiteSpace(Artist) ? Artist : "Unknown Artist";
        public string DisplayAlbum => !string.IsNullOrWhiteSpace(Album) ? Album : "Unknown Album";
        public string FileSizeString => FormatFileSize(FileSize);

        private LocalModel () {}

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