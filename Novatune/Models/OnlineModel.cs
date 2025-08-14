using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Novatune.Models
{
    public partial class OnlineModel : ObservableObject
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
