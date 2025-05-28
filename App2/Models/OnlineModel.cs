using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App2.Models
{
    public class OnlineModel : INotifyPropertyChanged
    {
        private string _title;
        private string _author;
        private string _duration;
        private string _thumbnailUrl;
        private string _videoId;
        private string _url;

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

        public string Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

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

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
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
}
