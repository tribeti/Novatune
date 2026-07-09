using DevWinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Novatune.App.Models;
using Novatune.App.Services;
using Novatune.App.ViewModels;
using Novatune.App.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinUIEx;

namespace Novatune.App;

public sealed partial class MainWindow : Window
{
    public MediaViewModel ViewModel { get; }

    public MainWindow()
    {
        // services
        ViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        var settingsService = App.Current.Services.GetService<SettingsService>()!;

        InitializeComponent();
        // title bar
        this.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        this.SetTitleBar(titleBar);
        // window
        this.CenterOnScreen();
        var manager = WinUIEx.WindowManager.Get(this);
        manager.MinWidth = 800;
        manager.MinHeight = 600;
        manager.Width = 1000;
        manager.Height = 800;
        //tray icon
        manager.IsVisibleInTray = true;
        manager.TrayIconContextMenu += (w, e) =>
        {
            var flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem() { Text = "Open" });
            flyout.Items.Add(new MenuFlyoutItem() { Text = "Quit" });
            ((MenuFlyoutItem) flyout.Items[0]).Click += (s, args) =>
            {
                this.Show();
                this.Activate();
            };

            ((MenuFlyoutItem) flyout.Items[1]).Click += (s, args) =>
            {
                this.Close();
            };
            e.Flyout = flyout;
        };

        this.AppWindow.Closing += (s, e) =>
        {
            if (settingsService.Settings.MinimizeOnClose)
            {
                e.Cancel = true;
                this.Hide();
            }
        };
    }

    private void Media_Timeline_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Slider slider)
            return;

        if (slider
            .FindDescendants()
            .OfType<Thumb>()
            .FirstOrDefault(x => x.Name == "HorizontalThumb") is not Thumb thumb)
        {
            return;
        }

        thumb.DragStarted -= Thumb_DragStarted;
        thumb.DragCompleted -= Thumb_DragCompleted;

        thumb.DragStarted += Thumb_DragStarted;
        thumb.DragCompleted += Thumb_DragCompleted;
    }

    private void NavigationBar_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected == true)
        {
            NavView_Navigate(typeof(SettingPage), args.RecommendedNavigationTransitionInfo);
        }
        else if (args.SelectedItemContainer is not null)
        {
            Type? navPageType = Type.GetType(args.SelectedItemContainer.Tag.ToString()!);
            NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }

    private void NavView_Navigate(Type? navPageType, NavigationTransitionInfo transitionInfo)
    {
        Type preNavPageType = ContentFrame.CurrentSourcePageType;
        if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
        {
            ContentFrame.Navigate(navPageType, null, transitionInfo);
        }
    }

    private void NavigationBar_Loaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigated += On_Navigated;
        NavigationBar.SelectedItem = NavigationBar.MenuItems[0];
        NavView_Navigate(typeof(HomePage), new EntranceNavigationTransitionInfo());
    }

    private void On_Navigated(object sender, NavigationEventArgs e)
    {
        NavigationBar.IsBackEnabled = ContentFrame.CanGoBack;

        if (ContentFrame.SourcePageType == typeof(SettingPage))
        {
            NavigationBar.SelectedItem = (NavigationViewItem) NavigationBar.SettingsItem;
        }
        else if (ContentFrame.SourcePageType is not null)
        {
            NavigationBar.SelectedItem = NavigationBar.MenuItems
                        .OfType<NavigationViewItem>()
                        .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName!.ToString()));
        }
    }

    private void Thumb_DragStarted(object sender, DragStartedEventArgs e) => ViewModel.IsUserInteracting = true;
    private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e) => ViewModel.CommitSeekCommand.Execute(null);
    private void Media_Timeline_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => ViewModel.CommitSeekCommand.Execute(null);

    private CancellationTokenSource? _searchCts;
    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        _searchCts?.Cancel();
        _searchCts?.Dispose();

        if (string.IsNullOrWhiteSpace(sender.Text))
        {
            _searchCts = null;
            sender.ItemsSource = Array.Empty<MediaItem>();
            return;
        }

        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(350, token);

            var stationsTask = RadioViewModel.SearchStationsAsync(sender.Text, token);
            var videosTask = YoutubeViewModel.SearchVideosAsync(sender.Text, token);
            
            await Task.WhenAll(stationsTask, videosTask);
            if (token.IsCancellationRequested)
                return;

            var stations = stationsTask.Result;
            var videos = videosTask.Result;

            var unifiedResults = new List<MediaItem>();

            foreach (var s in stations)
            {
                BitmapImage? img = null;
                if (!string.IsNullOrWhiteSpace(s.Favicon))
                {
                    var url = s.Favicon.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "https://" + s.Favicon[7..] : s.Favicon;
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        img = new BitmapImage(uri);
                }
                unifiedResults.Add(new MediaItem
                {
                    Kind = SourceKind.Radio,
                    Title = s.Name,
                    Subtitle = string.IsNullOrWhiteSpace(s.Tags) ? "Radio Station" : s.Tags,
                    Thumbnail = img ?? new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.png")),
                    SourceItem = s
                });
            }

            if (videos is not null)
            {
                foreach (var v in videos)
                {
                    BitmapImage? img = null;
                    if (!string.IsNullOrWhiteSpace(v.ThumbnailUrl))
                    {
                        if (Uri.TryCreate(v.ThumbnailUrl, UriKind.Absolute, out var uri))
                            img = new BitmapImage(uri);
                    }
                    unifiedResults.Add(new MediaItem
                    {
                        Kind = SourceKind.Youtube,
                        Title = v.Title,
                        Subtitle = v.Author,
                        Thumbnail = img ?? new BitmapImage(new Uri("ms-appx:///Assets/LockScreenLogo.png")),
                        SourceItem = v
                    });
                }
            }

            sender.ItemsSource = unifiedResults.Count > 0
                ? unifiedResults
                : new List<MediaItem> { new() { Title = "No results found", Subtitle = "Try different keywords", Kind = SourceKind.Local } };
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            sender.ItemsSource = Array.Empty<MediaItem>();
        }
        finally
        {
            if (_searchCts?.Token == token)
            {
                _searchCts.Dispose();
                _searchCts = null;
            }
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is MediaItem item)
        {
            if (item.Kind == SourceKind.Radio && item.SourceItem is RadioItem radio && !string.IsNullOrWhiteSpace(radio.UrlResolved))
            {
                ViewModel.AddRadio(radio);
            }
            else if (item.Kind == SourceKind.Youtube && item.SourceItem is YoutubeItem youtube)
            {
                ViewModel.AddYoutube(youtube);
            }

            sender.Text = string.Empty;
            sender.ItemsSource = Array.Empty<MediaItem>();
        }
    }
}
