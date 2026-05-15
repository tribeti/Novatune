using Avalonia.Controls;
using Avalonia.Interactivity;
using Novatune.Models;
using Novatune.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Novatune.Pages
{
    public partial class FolderDetailPage : UserControl
    {
        public MediaPlayerViewModel? MediaPlayerVM { get; private set; }
        public FolderViewModel FolderVM { get; private set; }
        public StorageFolder? SelectedFolder { get; private set; }

        private ObservableCollection<LocalFilesModel> allFiles = new();
        private ObservableCollection<LocalFilesModel> filteredFiles = new();
        private CancellationTokenSource? _filterCts;

        public FolderDetailPage ()
        {
            InitializeComponent();
            FolderVM = FolderViewModel.Instance;

            FolderVM.PropertyChanged += (s , e) =>
            {
                if ( e.PropertyName == nameof(FolderVM.Contents) ||
                    e.PropertyName == nameof(FolderVM.IsSearching) )
                {
                    UpdateFileCollections();
                }
            };

            FolderVM.Contents.CollectionChanged += (s , e) =>
            {
                UpdateFileCollections();
            };
            
            if ( SongList != null )
            {
                SongList.ItemsSource = filteredFiles;
                SongList.AddHandler(Control.TappedEvent, OnSongTapped, RoutingStrategies.Tunnel);
            }
        }

        private void UpdateFileCollections ()
        {
            var newItems = FolderVM.Contents ?? new ObservableCollection<LocalFilesModel>();
            for ( int i = allFiles.Count - 1 ; i >= 0 ; i-- )
            {
                if ( !newItems.Contains(allFiles [i]) )
                    allFiles.RemoveAt(i);
            }

            foreach ( var item in newItems )
            {
                if ( !allFiles.Contains(item) )
                    allFiles.Add(item);
            }

            ApplyCurrentFilter();
        }

        private void ApplyCurrentFilter ()
        {
            var filtered = allFiles.Where(FilterFile);
            ApplyFilterOptimized(filtered);
        }

        private void ApplyFilterOptimized (IEnumerable<LocalFilesModel> filteredData)
        {
            var filteredSet = new HashSet<LocalFilesModel>(filteredData);
            for ( int i = filteredFiles.Count - 1 ; i >= 0 ; i-- )
            {
                if ( !filteredSet.Contains(filteredFiles [i]) )
                    filteredFiles.RemoveAt(i);
            }
            foreach ( var item in filteredSet )
            {
                if ( !filteredFiles.Contains(item) )
                    filteredFiles.Add(item);
            }
        }

        public async Task SetupFolderContentAsync (StorageFolder folder)
        {
            try
            {
                await FolderVM.LoadSpecificFolderAsync(folder);
            }
            catch { }
        }

        private void BackButton_Click (object? sender , RoutedEventArgs e)
        {
            if ( FolderVM.IsSearching && FolderVM.CancelSearchCommand.CanExecute(null) )
            {
                FolderVM.CancelSearchCommand.Execute(null);
            }
        }

        private async void OnSongTapped (object? sender , Avalonia.Input.TappedEventArgs e)
        {
            if ( sender is Grid grid && grid.DataContext is LocalFilesModel audioModel && MediaPlayerVM is not null )
            {
                if ( MediaPlayerVM.PlayAudioCommand.CanExecute(audioModel) )
                {
                    try
                    {
                        await MediaPlayerVM.PlayAudioCommand.ExecuteAsync(audioModel);
                    }
                    catch ( Exception ex )
                    {
                        await DisplayPlaybackErrorDialog(audioModel.DisplayTitle , ex.Message);
                    }
                }
            }
        }

        private bool FilterFile (LocalFilesModel file)
        {
            string filterText = FilterByFirstName.Text ?? string.Empty;
            if ( string.IsNullOrWhiteSpace(filterText) )
                return true;
            bool matchesSongTitle = file.SongTitle?.Contains(filterText , StringComparison.InvariantCultureIgnoreCase) ?? false;
            bool matchesArtist = file.Artist?.Contains(filterText , StringComparison.InvariantCultureIgnoreCase) ?? false;
            bool matchesDisplayTitle = file.DisplayTitle?.Contains(filterText , StringComparison.InvariantCultureIgnoreCase) ?? false;

            return matchesSongTitle || matchesArtist || matchesDisplayTitle;
        }

        private async void OnFilterChanged (object? sender , TextChangedEventArgs args)
        {
            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;

            try
            {
                await Task.Delay(200 , token);
                if ( !token.IsCancellationRequested )
                    ApplyCurrentFilter();
            }
            catch ( TaskCanceledException ) { }
        }

        private async Task DisplayPlaybackErrorDialog (string audioTitle , string errorMessage)
        {
            await ShowErrorDialog("Lỗi phát media" , $"Không thể phát: {audioTitle}\nChi tiết: {errorMessage}");
        }

        private async Task ShowErrorDialog (string title , string content)
        {
            var window = TopLevel.GetTopLevel(this);
            var dialog = new Window
            {
                Width = 400,
                Height = 200,
                Title = title
            };
                
            var stackPanel = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 15
            };
            
            stackPanel.Children.Add(new TextBlock { Text = content, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            
            var closeButton = new Button 
            { 
                Content = "Đóng", 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right 
            };
            closeButton.Click += (s, e) => dialog.Close();
            stackPanel.Children.Add(closeButton);
            
            dialog.Content = stackPanel;
            
            if ( window != null )
            {
                await dialog.ShowDialog(window);
            }
        }

        protected override void OnAttachedToVisualTree (Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if ( FilterByFirstName != null )
            {
                FilterByFirstName.TextChanged += OnFilterChanged;
            }
        }

        protected override void OnDetachedFromVisualTree (Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if ( FolderVM.IsSearching && FolderVM.CancelSearchCommand.CanExecute(null) )
            {
                FolderVM.CancelSearchCommand.Execute(null);
            }
            if ( SelectedFolder is not null )
            {
                FolderVM.RemoveTemporaryFolder(SelectedFolder);
            }
            if ( FilterByFirstName != null )
            {
                FilterByFirstName.TextChanged -= OnFilterChanged;
            }
        }
    }
}