using Novatune.Controls;
using Novatune.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Novatune.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public FolderViewModel ViewModel { get; }
        public SettingsPage ()
        {
            this.InitializeComponent ();
            ViewModel = new FolderViewModel ();
            this.DataContext = ViewModel;
        }
        private void RemoveFolder_Click (object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is StorageFolder folder)
            {
                ViewModel.RemoveFolderCommand.Execute (folder);
            }
        }
    }
}