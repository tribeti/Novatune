using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Models;
using Novatune.App.ViewModels;

namespace Novatune.App.Views;

public sealed partial class TVPage : Page
{
    public MediaViewModel ViewModel { get; }
    private readonly LibraryViewModel _libraryViewModel;

    public TVPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        _libraryViewModel = App.Current.Services.GetRequiredService<LibraryViewModel>();
        mediaPlayerElement.SetMediaPlayer(ViewModel.MediaPlayer);
    }

    private void Queue_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is MediaItem selectedTrack)
        {
            ViewModel.PlayTrack(selectedTrack);
        }
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MediaItem item })
        {
            ViewModel.RemoveMedia(item);
        }
    }

    private void FavoriteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MediaItem item })
        {
            _ = _libraryViewModel.ToggleFavoriteAsync(item);
        }
    }

    private void ClearList_Click(object sender, RoutedEventArgs e) => ViewModel.ClearQueue(static k => k.Kind == SourceKind.TV);
}
