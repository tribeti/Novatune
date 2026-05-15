using Avalonia.Controls;
using Avalonia.Interactivity;
using Novatune.ViewModels;

namespace Novatune.Pages
{
    public partial class SettingsPage : UserControl
    {
        public FolderViewModel ViewModel => FolderViewModel.Instance;

        public SettingsPage()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void RemoveFolder_Click(object? sender, RoutedEventArgs e)
        {
            if ( sender is Button button && button.Tag is object folder )
            {
                ViewModel.RemoveFolderCommand.Execute(folder);
            }
        }
    }
}