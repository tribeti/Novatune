using Microsoft.UI.Xaml.Controls;
using Novatune.ViewModels;
using System.Linq;
using Windows.Storage;

namespace Novatune.Pages
{
    public sealed partial class HomePage : Page
    {
        public FolderViewModel ViewModel => FolderViewModel.Instance;

        public HomePage()
        {
            this.InitializeComponent();
            this.DataContext = new FolderViewModel();
        }

        private void Folders_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs e)
        {

            if (FoldersListView.SelectedItem is StorageFolder folder)
            { 
                if (ViewModel.Folders.Any (f => f.Path == folder.Path))
                {
                    Frame.Navigate (typeof (FolderDetailPage), folder);
                }
                else
                {
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack ();
                    }
                    else
                    {
                        Frame.Navigate (typeof (HomePage));
                    }
                }
            }
        }
    }
}