using CommunityToolkit.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Novatune.App.Models;
using Novatune.App.Services;
using Novatune.App.ViewModels;
using Novatune.App.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
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
        // slider
        this.Media_Timeline.Loaded += Media_Timeline_Loaded;
        // window
        this.AppWindow.SetIcon("Assets/icon.ico");
        this.AppWindow.SetTaskbarIcon("Assets/icon.ico");
        this.CenterOnScreen();
        var manager = WindowManager.Get(this);
        manager.MinWidth = 800;
        manager.MinHeight = 600;
        manager.Width = 1000;
        manager.Height = 800;
        //tray icon
        manager.IsVisibleInTray = true;
        manager.TrayIconContextMenu += (w, e) =>
        {
            var flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem { Text = ViewModel.IsPlaying ? "Pause" : "Play" });
            flyout.Items.Add(new MenuFlyoutItem() { Text = "Open" });
            flyout.Items.Add(new MenuFlyoutItem() { Text = "Quit" });
            ((MenuFlyoutItem) flyout.Items[0]).Click += (s, __) =>
            {
                ViewModel.PlayPause();
                ((MenuFlyoutItem) s).Text = ViewModel.IsPlaying ? "Pause" : "Play";
            };

            ((MenuFlyoutItem) flyout.Items[1]).Click += (s, args) =>
            {
                this.ShowAndActivate();
            };

            ((MenuFlyoutItem) flyout.Items[2]).Click += (s, args) =>
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
                _searchCts?.Cancel();
                _searchCts?.Dispose();
                _searchCts = null;
                _suggestion = null;
                SearchBox.ItemsSource = Array.Empty<MediaItem>();
                ContentFrame.Content = null;
                ContentFrame.BackStack.Clear();
                ContentFrame.ForwardStack.Clear();
                ViewModel.ReleaseForTray();
                this.Hide();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        };
    }

    public void ShowAndActivate()
    {
        if (ContentFrame.Content is null)
        {
            var pageType = typeof(HomePage);
            ContentFrame.Navigate(pageType, null, new EntranceNavigationTransitionInfo());
        }
        ViewModel.RestoreFromTray();
        this.Show();
        this.Activate();
    }

    private void Media_Timeline_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.Media_Timeline
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

    private static readonly Dictionary<string, Type> PageTypeMap = new()
    {
        [typeof(HomePage).FullName!] = typeof(HomePage),
        [typeof(LibraryPage).FullName!] = typeof(LibraryPage),
    };

    private void NavigationBar_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected == true)
        {
            NavView_Navigate(typeof(SettingPage), args.RecommendedNavigationTransitionInfo);
        }
        else if (args.SelectedItemContainer is not null)
        {
            var tag = args.SelectedItemContainer.Tag?.ToString();
            Type? navPageType = tag is not null && PageTypeMap.TryGetValue(tag, out var type) ? type : null;
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
            ContentFrame.BackStack.Clear();
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
    private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e) => ViewModel.CommitSeek();
    private void Media_Timeline_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => ViewModel.CommitSeek();
    private void Play_Btn_Click(object sender, RoutedEventArgs e) => ViewModel.PlayPause();
    private void Previous_Btn_Click(object sender, RoutedEventArgs e) => ViewModel.Previous();
    private void Next_Btn_Click(object sender, RoutedEventArgs e) => ViewModel.Next();
    private void Shuffle_Btn_Click(object sender, RoutedEventArgs e) => ViewModel.Shuffle();
    private void Repeat_Btn_Click(object sender, RoutedEventArgs e) => ViewModel.Repeat();
    private void Queue_Btn_Click(object sender, RoutedEventArgs e) => ViewModel.ToggleQueue();

    private CancellationTokenSource? _searchCts;
    private IEnumerable<MediaItem>? _suggestion;
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
        var cts = _searchCts;
        var token = cts.Token;

        try
        {
            var results = await SearchService.SearchAllAsync(sender.Text, token);
            if (!ReferenceEquals(_searchCts, cts))
                return;

            sender.ItemsSource = results.Count > 0
                ? results
                : new List<MediaItem> { new() { Title = "No results found", Subtitle = "Try different keywords", Kind = SourceKind.Local } };
            _suggestion = results;
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (!ReferenceEquals(_searchCts, cts))
                return;

            sender.ItemsSource = Array.Empty<MediaItem>();
            _suggestion = null;
        }
        finally
        {
            if (ReferenceEquals(_searchCts, cts))
            {
                cts.Dispose();
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
            else if (item.Kind == SourceKind.TV && item.SourceItem is IptvChannel tvChannel)
            {
                ViewModel.AddTV(tvChannel);
            }

            sender.Text = string.Empty;
            sender.ItemsSource = Array.Empty<MediaItem>();
        }
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs _)
    {
        var box = (AutoSuggestBox) sender;
        if (!string.IsNullOrEmpty(box.Text) && _suggestion is not null)
        {
            box.ItemsSource = _suggestion;
            box.IsSuggestionListOpen = true;
        }
    }

    private void AddtoQueue_Btn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not MediaItem item)
            return;

        if (item.Kind == SourceKind.Radio && item.SourceItem is RadioItem radio && !string.IsNullOrWhiteSpace(radio.UrlResolved))
        {
            ViewModel.AddRadioToQueue(radio);
        }
        else if (item.Kind == SourceKind.Youtube && item.SourceItem is YoutubeItem youtube)
        {
            ViewModel.AddYoutubeToQueue(youtube);
        }
        else if (item.Kind == SourceKind.TV && item.SourceItem is IptvChannel tvChannel)
        {
            ViewModel.AddTVToQueue(tvChannel);
        }
    }

    private async void Output_Box_Loaded(object _, RoutedEventArgs e)
    {
        var devices = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector());
        var defaultId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
        ComboBoxItem? defaultItem = null;

        if (Output_Box.Items.Count <= 0)
        {
            foreach (var device in devices)
            {
                var item = new ComboBoxItem
                {
                    Content = device.Name,
                    Tag = device
                };
                Output_Box.Items.Add(item);

                if (defaultItem is null && string.Equals(device.Id, defaultId, StringComparison.OrdinalIgnoreCase))
                {
                    defaultItem = item;
                }
            }
        }

        if (defaultItem is not null)
            Output_Box.SelectedItem = defaultItem;
    }

    private void Output_Box_SelectionChanged(object _, SelectionChangedEventArgs e)
    {
        DeviceInformation selectedDevice = (DeviceInformation) ((ComboBoxItem) Output_Box.SelectedItem).Tag;
        if (selectedDevice is not null)
        {
            ViewModel.MediaPlayer.AudioDevice = selectedDevice;
        }
    }

    private void PlaybackSpeedSlider_ValueChanged(object _, RangeBaseValueChangedEventArgs e) => ViewModel.MediaPlayer.PlaybackSession.PlaybackRate = PlaybackSpeedSlider.Value;

}