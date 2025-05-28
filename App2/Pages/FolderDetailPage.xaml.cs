using App2.Models;
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
                    await MediaPlayerVM.LoadAudioFilesAsync(SelectedFolder);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning: No StorageFolder parameter received in FolderDetailPage.");
                SelectedFolder = null;
                if (MediaPlayerVM != null)
                {
                    await MediaPlayerVM.LoadAudioFilesAsync(null);
                }
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
            if (e.ClickedItem is LocalAudioModel audioModel && MediaPlayerVM != null)
            {
                if (MediaPlayerVM.PlayAudioCommand.CanExecute(audioModel))
                {
                    try
                    {
                        await MediaPlayerVM.PlayAudioCommand.ExecuteAsync(audioModel);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi bắt đầu phát media: {ex.Message}");
                        DisplayPlaybackErrorDialog(audioModel.DisplayTitle, ex.Message);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ClickedItem không phải là LocalAudioModel hoặc MediaPlayerVM là null. ClickedItem type: {e.ClickedItem?.GetType().FullName}");
            }
        }

        private async void DisplayPlaybackErrorDialog(string audioTitle, string errorMessage)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Lỗi phát media",
                Content = $"Không thể phát: {audioTitle}\nChi tiết: {errorMessage}",
                CloseButtonText = "Đóng"
            };
            try
            {
                await errorDialog.ShowAsync();
            }
            catch (Exception dialogEx)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi hiển thị dialog: {dialogEx.Message}");
            }
        }
    }
}