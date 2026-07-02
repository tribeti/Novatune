using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Models;
using Novatune.App.ViewModels;

namespace Novatune.App.Views;

public sealed partial class HomePage : Page
{
    public MediaViewModel ViewModel { get; }
    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        DataContext = ViewModel;

        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => mediaPlayerElement.SetMediaPlayer(ViewModel.MediaPlayer);

    private void OnUnloaded(object sender, RoutedEventArgs e) => mediaPlayerElement.SetMediaPlayer(null);

    private void LocalList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is MediaItem selectedTrack)
        {
            ViewModel.PlayTrack(selectedTrack);
        }
    }
}
