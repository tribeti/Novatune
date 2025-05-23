using App2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.Storage;

namespace App2.Pages
{
    public sealed partial class FolderDetailPage : Page
    {
        public MediaPlayerViewModel MediaPlayerVM { get; private set; }
        public StorageFolder SelectedFolder { get; private set; }
        public FolderDetailPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var mainWindow = App.MainWindow as MainWindow;
            if (mainWindow == null || mainWindow.GlobalMediaPlayerVM == null)
            {
                System.Diagnostics.Debug.WriteLine("Critical Error: Could not access MainWindow or GlobalMediaPlayerVM from FolderDetailPage.");
                if (Frame.CanGoBack) Frame.GoBack();
                return;
            }
            MediaPlayerVM = mainWindow.GlobalMediaPlayerVM;

            if (e.Parameter is StorageFolder folder)
            {
                SelectedFolder = folder;
                if (MediaPlayerVM != null)
                {
                    await MediaPlayerVM.LoadMediaItemsAsync(SelectedFolder);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning: No StorageFolder parameter received in FolderDetailPage.");
                SelectedFolder = null;
                MediaPlayerVM?.MediaItems.Clear();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is StorageFile file && MediaPlayerVM != null)
            {
                if (MediaPlayerVM.PlayMediaFileCommand.CanExecute(file))
                {
                    try
                    {
                        await MediaPlayerVM.PlayMediaFileCommand.ExecuteAsync(file);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error initiating media playback: {ex.Message}");
                        DisplayPlaybackErrorDialog(file.Name, ex.Message);
                    }
                }
            }
        }

        private async void DisplayPlaybackErrorDialog(string fileName, string errorMessage)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Lỗi phát media",
                Content = $"Không thể phát file: {fileName}\nChi tiết: {errorMessage}",
                CloseButtonText = "Đóng"
            };
            await errorDialog.ShowAsync();
        }
    }
}