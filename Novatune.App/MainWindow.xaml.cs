using DevWinUI;
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
            sender.ItemsSource = Array.Empty<RadioItem>();
            return;
        }

        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(350, token);
            var stations = await RadioViewModel.SearchStationsAsync(sender.Text, token);

            sender.ItemsSource = stations.Count > 0
                ? stations
                : new List<RadioItem> { new() { Name = "No results found" } };
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            sender.ItemsSource = Array.Empty<RadioItem>();
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

    private void SearchBox_QuerySubmitted(AutoSuggestBox _, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is not RadioItem station || string.IsNullOrWhiteSpace(station.UrlResolved))
            return;

        ViewModel.AddRadio(station);
    }
}
