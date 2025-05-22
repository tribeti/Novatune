using App2.ViewModels; // Đảm bảo using này tồn tại và đúng
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.Storage; // For StorageFolder, StorageFile

namespace App2.Pages
{
    public sealed partial class FolderDetailPage : Page
    {
        // ViewModel property for x:Bind
        public MediaPlayerViewModel MediaPlayerVM { get; private set; }
        // Property for displaying folder name via x:Bind
        public StorageFolder SelectedFolder { get; private set; }
        // If using INotifyPropertyChanged for SelectedFolder for x:Bind updates (not strictly necessary if set in OnNavigatedTo before UI renders):
        // public StorageFolder SelectedFolder { get => _selectedFolder; private set => SetProperty(ref _selectedFolder, value); }
        // private StorageFolder _selectedFolder;


        public FolderDetailPage()
        {
            this.InitializeComponent();
            // ViewModel is typically set in OnNavigatedTo or injected
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // It's crucial to get the correct MainWindow instance.
            // App.MainWindow approach is common. Ensure App.MainWindow is correctly typed.
            var mainWindow = App.MainWindow as MainWindow;
            if (mainWindow == null || mainWindow.GlobalMediaPlayerVM == null)
            {
                System.Diagnostics.Debug.WriteLine("Critical Error: Could not access MainWindow or GlobalMediaPlayerVM from FolderDetailPage.");
                // Optionally, navigate back or show an error message to the user
                if (Frame.CanGoBack) Frame.GoBack();
                return;
            }
            MediaPlayerVM = mainWindow.GlobalMediaPlayerVM;

            if (e.Parameter is StorageFolder folder)
            {
                SelectedFolder = folder;
                // If SelectedFolder is an ObservableProperty or implements INPC and this page is its own DataContext,
                // UI bound to SelectedFolder.Name will update. x:Bind often handles this directly for Page properties.
                // Manually trigger PropertyChanged if necessary: OnPropertyChanged(nameof(SelectedFolder));

                // Load media items for the selected folder
                if (MediaPlayerVM != null)
                {
                    await MediaPlayerVM.LoadMediaItemsAsync(SelectedFolder);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning: No StorageFolder parameter received in FolderDetailPage.");
                SelectedFolder = null; // Ensure SelectedFolder.Name binding doesn't show old data
                // OnPropertyChanged(nameof(SelectedFolder));
                MediaPlayerVM?.MediaItems.Clear(); // Clear list if no folder
            }
            // DataContext is implicitly this page, so x:Bind works on public properties of the Page.
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
                // Check if the command can execute before attempting
                if (MediaPlayerVM.PlayMediaFileCommand.CanExecute(file))
                {
                    try
                    {
                        await MediaPlayerVM.PlayMediaFileCommand.ExecuteAsync(file); // Use ExecuteAsync for RelayCommand<T>
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
                XamlRoot = this.XamlRoot, // Important for WinUI 3 dialogs
                Title = "Lỗi phát media",
                Content = $"Không thể phát file: {fileName}\nChi tiết: {errorMessage}",
                CloseButtonText = "Đóng"
            };
            await errorDialog.ShowAsync();
        }

        // Implement INotifyPropertyChanged if you make SelectedFolder a property that needs to notify changes manually
        // (e.g., if not relying solely on OnNavigatedTo for updates before render)
        // public event PropertyChangedEventHandler PropertyChanged;
        // private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        // {
        //     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        // }
        // private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        // {
        //    if (Equals(storage, value)) return false;
        //    storage = value;
        //    OnPropertyChanged(propertyName);
        //    return true;
        // }
    }
}