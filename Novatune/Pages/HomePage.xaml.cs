using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Novatune.ViewModels;
using System.Linq;
using Windows.Storage;

namespace Novatune.Pages
{
    public partial class HomePage : UserControl
    {
        public FolderViewModel ViewModel => FolderViewModel.Instance;

        public HomePage ()
        {
            InitializeComponent();
            this.DataContext = new FolderViewModel();
            
            if ( FoldersListView != null )
            {
                FoldersListView.AddHandler(InputElement.TappedEvent, OnFolderTapped, RoutingStrategies.Tunnel);
            }
        }

        private void OnFolderTapped (object? sender , TappedEventArgs e)
        {
            if ( sender is Border border && border.Tag is StorageFolder folder )
            {
                if ( ViewModel.Folders.Any(f => f.Path == folder.Path) )
                {
                    // Navigate to FolderDetailPage - need to implement navigation for Avalonia
                    // For now, we'll just select the folder
                    System.Diagnostics.Debug.WriteLine($"Navigate to folder: {folder.Path}");
                }
            }
        }
        
        private void Folder_Click (object? sender , RoutedEventArgs e)
        {
            if ( sender is Border border && border.Tag is StorageFolder folder )
            {
                if ( ViewModel.Folders.Any(f => f.Path == folder.Path) )
                {
                    System.Diagnostics.Debug.WriteLine($"Navigate to folder: {folder.Path}");
                }
            }
        }
    }
}