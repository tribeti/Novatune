using App2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

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
                await LoadFolderItems(folder);
            }
        }

        private async Task LoadFolderItems(StorageFolder folder)
        {
            var items = await folder.GetItemsAsync();
            FolderItems.Clear();
            foreach (var item in items)
            {
                FolderItems.Add(item);
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
