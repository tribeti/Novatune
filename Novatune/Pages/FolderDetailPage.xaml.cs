﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Novatune.Models;
using Novatune.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Novatune.Pages
{
    public sealed partial class FolderDetailPage : Page
    {
        public MediaPlayerViewModel? MediaPlayerVM { get; private set; }
        public FolderViewModel FolderVM { get; private set; }
        public StorageFolder? SelectedFolder { get; private set; }
        public bool ShowEmptyState => FolderVM?.Contents?.Count == 0 && !FolderVM.IsSearching;

        private ObservableCollection<LocalModel> allFiles = new ObservableCollection<LocalModel> ();
        private ObservableCollection<LocalModel> filteredFiles = new ObservableCollection<LocalModel> ();

        public FolderDetailPage ()
        {
            this.InitializeComponent ();
            FolderVM = FolderViewModel.Instance;

            FolderVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof (FolderVM.Contents) ||
                    e.PropertyName == nameof (FolderVM.IsSearching))
                {
                    UpdateFileCollections ();
                    this.Bindings.Update ();
                }
            };

            FolderVM.Contents.CollectionChanged += (s, e) =>
            {
                UpdateFileCollections ();
                this.Bindings.Update ();
            };
            FileListView.ItemsSource = filteredFiles;
        }

        private void UpdateFileCollections ()
        {
            allFiles.Clear ();
            if (FolderVM.Contents != null)
            {
                foreach (var item in FolderVM.Contents)
                {
                    allFiles.Add (item);
                }
            }
            ApplyCurrentFilter ();
        }

        private void ApplyCurrentFilter ()
        {
            var filtered = allFiles.Where (file => FilterFile (file));
            RemoveNonMatchingFiles (filtered);
            AddBackFiles (filtered);
        }

        protected override async void OnNavigatedTo (NavigationEventArgs e)
        {
            base.OnNavigatedTo (e);

            var mainWindow = App.MainWindow as MainWindow;
            if (mainWindow == null || mainWindow.GlobalMediaPlayerVM == null)
            {
                System.Diagnostics.Debug.WriteLine ("Critical Error: Could not access MainWindow or GlobalMediaPlayerVM from FolderDetailPage.");
                if (Frame.CanGoBack)
                    Frame.GoBack ();
                return;
            }
            MediaPlayerVM = mainWindow.GlobalMediaPlayerVM;

            if (e.Parameter is StorageFolder folder)
            {
                SelectedFolder = folder;
                await SetupFolderContentAsync (folder);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine ("Warning: No StorageFolder parameter received in FolderDetailPage.");
                SelectedFolder = null;
                FolderVM.Contents.Clear ();
            }
            this.Bindings.Update ();
        }

        private async Task SetupFolderContentAsync (StorageFolder folder)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine ($"Setting up folder content for: {folder.Name}");
                await FolderVM.LoadSpecificFolderAsync (folder);
                System.Diagnostics.Debug.WriteLine ($"Setup complete. Found {FolderVM.Contents.Count} files");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine ($"Error setting up folder content: {ex.Message}");
                await ShowErrorDialog ("Lỗi tải thư mục", $"Không thể tải nội dung thư mục: {ex.Message}");
            }
        }

        private void BackButton_Click (object sender, RoutedEventArgs e)
        {
            if (FolderVM.IsSearching && FolderVM.CancelSearchCommand.CanExecute (null))
            {
                FolderVM.CancelSearchCommand.Execute (null);
            }

            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack ();
            }
        }

        private async void FileListView_ItemClick (object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is LocalModel audioModel && MediaPlayerVM is not null)
            {
                if (MediaPlayerVM.PlayAudioCommand.CanExecute (audioModel))
                {
                    try
                    {
                        await MediaPlayerVM.PlayAudioCommand.ExecuteAsync (audioModel);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine ($"Lỗi khi bắt đầu phát media: {ex.Message}");
                        await DisplayPlaybackErrorDialog (audioModel.DisplayTitle, ex.Message);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine ("PlayAudioCommand cannot execute for the selected audio model.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine ($"ClickedItem không phải là LocalModel hoặc MediaPlayerVM là null. ClickedItem type: {e.ClickedItem?.GetType ().FullName}");
            }
        }

        private void OnFilterChanged (object sender, TextChangedEventArgs args)
        {

            var filtered = allFiles.Where (file => FilterFile (file));
            RemoveNonMatchingFiles (filtered);
            AddBackFiles (filtered);
        }

        private bool FilterFile (LocalModel file)
        {
            string filterText = FilterByFirstName.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace (filterText))
                return true;
            bool matchesSongTitle = file.SongTitle?.Contains (filterText, StringComparison.InvariantCultureIgnoreCase) ?? false;
            bool matchesArtist = file.Artist?.Contains (filterText, StringComparison.InvariantCultureIgnoreCase) ?? false;
            bool matchesDisplayTitle = file.DisplayTitle?.Contains (filterText, StringComparison.InvariantCultureIgnoreCase) ?? false;

            return matchesSongTitle || matchesArtist || matchesDisplayTitle;
        }

        private void RemoveNonMatchingFiles (IEnumerable<LocalModel> filteredData)
        {
            for (int i = filteredFiles.Count - 1; i >= 0; i--)
            {
                var item = filteredFiles[i];
                if (!filteredData.Contains (item))
                {
                    filteredFiles.Remove (item);
                }
            }
        }

        private void AddBackFiles (IEnumerable<LocalModel> filteredData)
        {
            foreach (var item in filteredData)
            {
                if (!filteredFiles.Contains (item))
                {
                    filteredFiles.Add (item);
                }
            }
        }

        private async Task DisplayPlaybackErrorDialog (string audioTitle, string errorMessage)
        {
            await ShowErrorDialog ("Lỗi phát media", $"Không thể phát: {audioTitle}\nChi tiết: {errorMessage}");
        }

        private async Task ShowErrorDialog (string title, string content)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = title,
                Content = content,
                CloseButtonText = "Đóng"
            };

            try
            {
                await errorDialog.ShowAsync ();
            }
            catch (Exception dialogEx)
            {
                System.Diagnostics.Debug.WriteLine ($"Lỗi khi hiển thị dialog: {dialogEx.Message}");
            }
        }

        protected override void OnNavigatedFrom (NavigationEventArgs e)
        {
            base.OnNavigatedFrom (e);
            if (FolderVM.IsSearching && FolderVM.CancelSearchCommand.CanExecute (null))
            {
                FolderVM.CancelSearchCommand.Execute (null);
            }
            if (SelectedFolder is not null)
            {
                FolderVM.RemoveTemporaryFolder (SelectedFolder);
            }
        }
    }
}