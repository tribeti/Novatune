using System;
using Novatune.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Novatune.Pages
{
    public sealed partial class HomePage : Page
    {
        public FolderViewModel ViewModel { get; }

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