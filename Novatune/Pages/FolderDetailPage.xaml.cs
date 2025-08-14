using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
    public sealed partial class FolderDetailPage : Page
    {
        public MediaPlayerViewModel? MediaPlayerVM { get; private set; }
        public FolderViewModel FolderVM { get; private set; }
        public StorageFolder? SelectedFolder { get; private set; }

        private ObservableCollection<LocalFilesModel> allFiles = new();
        private ObservableCollection<LocalFilesModel> filteredFiles = new();
        private CancellationTokenSource? _filterCts;

        // TODO : optimize
        public FolderDetailPage ()
        {
            this.InitializeComponent();
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
            SongList.ItemsSource = filteredFiles;
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

        protected override async void OnNavigatedTo (NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var mainWindow = App.MainWindow as MainWindow;
            if ( mainWindow == null || mainWindow.GlobalMediaPlayerVM == null )
            {
                if ( Frame.CanGoBack )
                    Frame.GoBack();
                return;
            }
            MediaPlayerVM = mainWindow.GlobalMediaPlayerVM;

            if ( e.Parameter is StorageFolder folder )
            {
                SelectedFolder = folder;
                FilterByFirstName.Text = null;
                await SetupFolderContentAsync(folder);
            }
            else
            {
                SelectedFolder = null;
                FolderVM.Contents.Clear();
            }
        }

        private async Task SetupFolderContentAsync (StorageFolder folder)
        {
            try
            {
                await FolderVM.LoadSpecificFolderAsync(folder);
            }
            catch { }
        }

        private void BackButton_Click (object sender , RoutedEventArgs e)
        {
            if ( FolderVM.IsSearching && FolderVM.CancelSearchCommand.CanExecute(null) )
            {
                FolderVM.CancelSearchCommand.Execute(null);
            }

            if ( this.Frame.CanGoBack )
            {
                this.Frame.GoBack();
            }
        }

        private async void SongList_ItemClick (object sender , ItemClickEventArgs e)
        {
            if ( e.ClickedItem is LocalFilesModel audioModel && MediaPlayerVM is not null )
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

        private async void OnFilterChanged (object sender , TextChangedEventArgs args)
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

        private void RemoveNonMatchingFiles (IEnumerable<LocalFilesModel> filteredData)
        {
            for ( int i = filteredFiles.Count - 1 ; i >= 0 ; i-- )
            {
                var item = filteredFiles [i];
                if ( !filteredData.Contains(item) )
                {
                    filteredFiles.Remove(item);
                }
            }
        }

        private void AddBackFiles (IEnumerable<LocalFilesModel> filteredData)
        {
            foreach ( var item in filteredData )
            {
                if ( !filteredFiles.Contains(item) )
                {
                    filteredFiles.Add(item);
                }
            }
        }

        private async Task DisplayPlaybackErrorDialog (string audioTitle , string errorMessage)
        {
            await ShowErrorDialog("Lỗi phát media" , $"Không thể phát: {audioTitle}\nChi tiết: {errorMessage}");
        }

        private async Task ShowErrorDialog (string title , string content)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot ,
                Title = title ,
                Content = content ,
                CloseButtonText = "Đóng"
            };

            try
            {
                await errorDialog.ShowAsync();
            }
            catch ( Exception ) { }
        }

        protected override void OnNavigatedFrom (NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if ( FolderVM.IsSearching && FolderVM.CancelSearchCommand.CanExecute(null) )
            {
                FolderVM.CancelSearchCommand.Execute(null);
            }
            if ( SelectedFolder is not null )
            {
                FolderVM.RemoveTemporaryFolder(SelectedFolder);
            }
        }
    }
}