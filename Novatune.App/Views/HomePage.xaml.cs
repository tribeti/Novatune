using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        if (Queue.SelectedIndex >= 0 && Queue.SelectedIndex != ViewModel.LocalPlaylistIndex)
        {
            ViewModel.PlayLocal(Queue.SelectedIndex);
            RadioList.SelectedIndex = -1;
        }
    }

    private void RadioList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RadioList.SelectedIndex >= 0 && RadioList.SelectedIndex != ViewModel.RadioPlaylistIndex)
        {
            ViewModel.PlayRadio(RadioList.SelectedIndex);
            Queue.SelectedIndex = -1;
        }
    }
}
