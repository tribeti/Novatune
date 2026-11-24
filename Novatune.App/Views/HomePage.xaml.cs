using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Models;
using Novatune.App.Services;
using Novatune.App.ViewModels;

namespace Novatune.App.Views;

public sealed partial class HomePage : Page
{
    public MediaViewModel ViewModel { get; }
    private readonly LibraryViewModel _libraryViewModel;

    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        _libraryViewModel = App.Current.Services.GetRequiredService<LibraryViewModel>();
        DataContext = ViewModel;
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

    private void ClearList_Click(object sender, RoutedEventArgs e) => ViewModel.ClearPlaylist();

    public static string FavoriteGlyph(bool isFavorite) => isFavorite ? "\uEB52" : "\uEB51";

    public static string FavoriteLabel(bool isFavorite) => Strings.Get(isFavorite ? "FavoriteRemove" : "FavoriteAdd");
}
