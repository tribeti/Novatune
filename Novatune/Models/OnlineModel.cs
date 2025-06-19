using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Novatune.Models
{
    public class OnlineModel : INotifyPropertyChanged
    {
        private string _title;
        private string _author;
        private TimeSpan? _durationTimeSpan;
        private string _thumbnailUrl;
        private string _videoId;
        private string _streamUrl;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public TimeSpan? DurationTimeSpan
        {
            get => _durationTimeSpan;
            set
            {
                if (SetProperty(ref _durationTimeSpan, value))
                {
                    OnPropertyChanged(nameof(DurationDisplay));
                }
            }
        }
        public string DurationDisplay => DurationTimeSpan.HasValue ? FormatDurationStatic(DurationTimeSpan.Value) : "N/A";

        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set => SetProperty(ref _thumbnailUrl, value);
        }

        public string VideoId
        {
            get => _videoId;
            set => SetProperty(ref _videoId, value);
        }

        public string StreamUrl
        {
            get => _streamUrl;
            set => SetProperty(ref _streamUrl, value);
        }

        public string DisplayTitle => !string.IsNullOrWhiteSpace(Title) ? Title : "Không rõ tiêu đề";
        public string DisplayArtist => !string.IsNullOrWhiteSpace(Author) ? Author : "Không rõ nghệ sĩ";

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

        private static string FormatDurationStatic(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return duration.ToString(@"h\:mm\:ss");
            }
            return duration.ToString(@"m\:ss");
        }
    }
}
