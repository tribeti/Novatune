using App2.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace App2.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            this.DataContext = new FolderViewModel();
        }
    }
}
