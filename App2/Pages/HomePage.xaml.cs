using System;
using Windows.Graphics.Printing.PrintSupport;
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
            if (ViewModel == null) return;

            ViewModel.SelectedFolders.Clear();

            foreach (var item in sender.SelectedItems.OfType<StorageFolder>())
            {
                ViewModel.SelectedFolders.Add(item);
            }

            await ViewModel.UpdateContentsAsync();
        }
    }
}