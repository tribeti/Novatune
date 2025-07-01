using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Novatune.Models
{
    public partial class OnlineModel : ObservableObject, INotifyPropertyChanged
    {
        [ObservableProperty]
        public partial string Title { get; set; }

        [ObservableProperty]
        public partial string Author { get; set; }

        [ObservableProperty]
        public partial string ThumbnailUrl { get; set; }
        
        [ObservableProperty]
        public partial string VideoId { get; set; }
        
        [ObservableProperty]
        public partial string StreamUrl { get; set; }

        private TimeSpan? _durationTimeSpan;
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

        public string DisplayTitle => !string.IsNullOrWhiteSpace(Title) ? Title : "Unknown Title";
        public string DisplayArtist => !string.IsNullOrWhiteSpace(Author) ? Author : "Unknown Artist";

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
