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

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Queue.SelectedIndex >= 0 && Queue.SelectedIndex != ViewModel.PlaylistIndex)
        {
            ViewModel.JumpTo(Queue.SelectedIndex);
            ViewModel.mediaPlayer.Play();
        }
    }
}
