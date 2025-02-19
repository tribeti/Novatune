using System;
using App2.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Windows.Storage;

namespace App2.Pages
{
    public sealed partial class HomePage : Page
    {
        private FolderViewModel ViewModel => DataContext as FolderViewModel;

        public HomePage()
        {
            this.InitializeComponent();

            this.DataContext = new FolderViewModel();
        }

        private async void Folders_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs e)
        {
            if (FoldersListView.SelectedItem is StorageFolder folder)
            {
                Frame.Navigate(typeof(FolderDetailPage), folder);
            }
        }
    }
}