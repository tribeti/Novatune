using App2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace App2.Pages
{
    public sealed partial class OnlinePage : Page
    {
        public OnlineViewModel ViewModel { get; }
        public OnlinePage()
        {
            this.InitializeComponent();
            ViewModel = new OnlineViewModel();
        }

        private async void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (ViewModel.SearchCommand.CanExecute(null))
                {
                    ViewModel.SearchCommand.Execute(null);
                }
            }
        }
    }
}
