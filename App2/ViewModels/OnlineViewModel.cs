using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using App2.Models;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace App2.ViewModels
{
    public class OnlineViewModel : INotifyPropertyChanged
    {
        private readonly YoutubeClient _youtubeClient;
        private string _searchQuery;
        private bool _isLoading;

        public OnlineViewModel()
        {
            _youtubeClient = new YoutubeClient();
            Videos = new ObservableCollection<OnlineModel>();
            SearchCommand = new RelayCommand(async () => await SearchVideosAsync());
        }

        public ObservableCollection<OnlineModel> Videos { get; }

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand SearchCommand { get; }

        private async Task SearchVideosAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
                return;

            IsLoading = true;
            Videos.Clear();

            try
            {
                // Tìm kiếm video trên YouTube
                var searchResults = _youtubeClient.Search.GetVideosAsync(SearchQuery);
                await foreach (var video in searchResults)
                {
                    var videoModel = new OnlineModel
                    {
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        Duration = FormatDuration(video.Duration),
                        ThumbnailUrl = video.Thumbnails.GetWithHighestResolution()?.Url ?? "",
                        VideoId = video.Id,
                        Url = video.Url
                    };

                    Videos.Add(videoModel);
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string FormatDuration(TimeSpan? duration)
        {
            if (!duration.HasValue)
                return "Unknown";

            var time = duration.Value;
            if (time.Hours > 0)
                return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            else
                return $"{time.Minutes:D2}:{time.Seconds:D2}";
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
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object parameter) => await _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
