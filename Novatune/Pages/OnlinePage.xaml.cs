﻿using System;
using Novatune.Models;
using Novatune.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace Novatune.Pages
{
    public sealed partial class OnlinePage : Page
    {
        public OnlineViewModel ViewModel { get; private set; }
        public OnlinePage()
        {
            this.InitializeComponent();

        }

        private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (ViewModel.SearchCommand.CanExecute(null))
                {
                    ViewModel.SearchCommand.Execute(null);
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var mainWindow = App.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.GlobalMediaPlayerVM != null)
            {
                ViewModel = new OnlineViewModel(mainWindow.GlobalMediaPlayerVM);
                this.DataContext = ViewModel;
            }
        }

        private async void VideosListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is OnlineModel selectedVideo && ViewModel != null)
            {
                if (ViewModel.PlayVideoCommand.CanExecute(selectedVideo))
                {
                    await ViewModel.PlayVideoCommand.ExecuteAsync(selectedVideo);
                }
            }
        }
    }
}
