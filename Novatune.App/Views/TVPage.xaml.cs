using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.ViewModels;

namespace Novatune.App.Views;

public sealed partial class TVPage : Page
{
    public MediaViewModel ViewModel { get; }

    public TVPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        mediaPlayerElement.SetMediaPlayer(ViewModel.MediaPlayer);
    }
}
