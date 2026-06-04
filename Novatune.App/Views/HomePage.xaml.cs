using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Novatune.App.ViewModels;
using System;
using Windows.Media.Core;

namespace Novatune.App.Views;

public sealed partial class HomePage : Page
{
    public MediaViewModel ViewModel { get; set; }
    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        mediaPlayerElement.SetMediaPlayer(ViewModel.Player);
        ViewModel.Player.Source = MediaSource.CreateFromUri(new Uri("D:\\VS\\tribeti\\Novatune\\Novatune.App\\Assets\\audio.mp3"));
    }
}
