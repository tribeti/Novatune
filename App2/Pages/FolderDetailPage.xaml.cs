using App2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace App2.Pages
{
    public sealed partial class FolderDetailPage : Page
    {
        public FolderViewModel FolderVM { get; set; }
        public MediaPlayerViewModel MediaPlayerVM { get; set; }
        public StorageFolder SelectedFolder { get; set; }
        public ObservableCollection<IStorageItem> FolderItems { get; set; } = new ObservableCollection<IStorageItem>();

        public FolderDetailPage()
        {
            this.InitializeComponent();
            FolderVM = new FolderViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Lấy MainWindow để truy cập MediaPlayerViewModel toàn cục
            var mainWindow = App.MainWindow as MainWindow;
            if (mainWindow == null) return;

            MediaPlayerVM = mainWindow.GlobalMediaPlayerVM;
            this.DataContext = MediaPlayerVM;

            if (e.Parameter is StorageFolder folder)
            {
                SelectedFolder = folder;
                await MediaPlayerVM.LoadMediaItemsAsync(SelectedFolder);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is StorageFile file)
            {
                try
                {
                    await MediaPlayerVM.PlayMediaFileAsync(file);
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi khi phát file
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Lỗi phát media",
                        Content = $"Không thể phát file: {ex.Message}",
                        CloseButtonText = "OK"
                    };

                    await errorDialog.ShowAsync();
                }
            }
        }
    }
}