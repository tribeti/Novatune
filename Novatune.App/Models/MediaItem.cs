using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Playback;

namespace Novatune.App.Models;

public partial class MediaItem : ObservableObject
{
    public MediaPlaybackItem PlaybackItem { get; init; } = null!;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Artist { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    [ObservableProperty]
    public partial BitmapImage? Img { get; set; }
}
