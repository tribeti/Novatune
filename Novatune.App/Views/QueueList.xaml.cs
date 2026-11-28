using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Models;
using Novatune.App.Services;
using Novatune.App.ViewModels;

namespace Novatune.App.Views;

/// <summary>
/// Shared queue list used by HomePage and TVPage: rows with remove/favourite
/// actions; selecting a row plays it.
/// </summary>
public sealed partial class QueueList : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(QueueList), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedTrackProperty =
        DependencyProperty.Register(nameof(SelectedTrack), typeof(object), typeof(QueueList), new PropertyMetadata(null));

    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedTrack
    {
        get => GetValue(SelectedTrackProperty);
        set => SetValue(SelectedTrackProperty, value);
    }

    public QueueList()
    {
        InitializeComponent();
    }

    private void Queue_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is MediaItem selectedTrack)
            App.Current.Services.GetRequiredService<MediaViewModel>().PlayTrack(selectedTrack);
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MediaItem item })
            App.Current.Services.GetRequiredService<MediaViewModel>().RemoveMedia(item);
    }

    private void FavoriteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MediaItem item })
            _ = App.Current.Services.GetRequiredService<LibraryViewModel>().ToggleFavoriteAsync(item);
    }

    public static string FavoriteGlyph(bool isFavorite) => isFavorite ? "\uEB52" : "\uEB51";

    public static string FavoriteLabel(bool isFavorite) => Strings.Get(isFavorite ? "FavoriteRemove" : "FavoriteAdd");
}
