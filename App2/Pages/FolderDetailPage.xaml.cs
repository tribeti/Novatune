using App2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App2.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FolderDetailPage : Page
    {
        public FolderViewModel ViewModel { get; set; }

        public StorageFolder SelectedFolder { get; set; }
        public ObservableCollection<IStorageItem> FolderItems { get; set; } = new ObservableCollection<IStorageItem>();
        public FolderDetailPage()
        {
            this.InitializeComponent();
            ViewModel = new FolderViewModel();
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is StorageFolder folder)
            {
                SelectedFolder = folder;
                await GetFilesInFolderAsync(folder);
            }
        }

        private async Task GetFilesInFolderAsync(StorageFolder folder)
        {
            var multimediaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".mp4", ".acc", ".aac", ".flac"
            };

            FolderItems.Clear();

            var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, multimediaExtensions.ToList());
            queryOptions.FolderDepth = FolderDepth.Deep;

            var query = folder.CreateItemQueryWithOptions(queryOptions);
            var items = await query.GetItemsAsync();

            foreach (var item in items)
            {
                if (item is StorageFile file && multimediaExtensions.Contains(file.FileType))
                {
                    FolderItems.Add(item);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
