using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.ViewModels;

namespace Novatune.App.Views;

public sealed partial class HomePage : Page
{
    public MediaViewModel ViewModel { get; set; }
    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        DataContext = ViewModel;
        mediaPlayerElement.SetMediaPlayer(ViewModel.mediaPlayer);
    }

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
