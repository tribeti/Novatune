using System;
using System.Linq;
using Novatune.UI;
using Novatune.Pages;
using Novatune.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;

namespace Novatune
{
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public MediaPlayerViewModel? GlobalMediaPlayerVM { get; private set; }
        public MediaControlsView GlobalMediaControlsPublic => this.GlobalMediaControls;
        public AppWindowTitleBar? TitleBar { get; private set; }
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Novatune";

            if (this.GlobalMediaControls == null)
            {
                return;
            }

            GlobalMediaPlayerVM = new ();
            this.GlobalMediaControls.Initialize(GlobalMediaPlayerVM);

            this.MinHeight = 600;
            this.MinWidth = 1000;
            this.PersistenceId = "MainWindow";

            this.Closed += MainWindow_Closed;

            ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
            this.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

            ContentFrame.Navigate(typeof(HomePage), null, new DrillInNavigationTransitionInfo());
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems.FirstOrDefault();

            ContentFrame.Navigated += ContentFrame_Navigated;
            RootNavigationView.Loaded += RootNavigationView_Loaded;
            RootNavigationView.BackRequested += RootNavigationView_BackRequested;
        }

        private void RootNavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            var homeItem = RootNavigationView.MenuItems.OfType<NavigationViewItem>()
                                             .FirstOrDefault(item => item.Tag?.ToString() == typeof(HomePage).FullName);
            if (homeItem != null)
            {
                RootNavigationView.SelectedItem = homeItem;
                ContentFrame.Navigate(typeof(HomePage));
            }
            else if (RootNavigationView.MenuItems.Count > 0 && RootNavigationView.MenuItems[0] is NavigationViewItem firstItem)
            {
                RootNavigationView.SelectedItem = firstItem;
                if (firstItem.Tag is string tag && !string.IsNullOrEmpty(tag))
                {
                    Type? pageType = Type.GetType(tag);
                    if (pageType != null) ContentFrame.Navigate(pageType);
                }
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            RootNavigationView.IsBackEnabled = ContentFrame.CanGoBack;
            if (this.GlobalMediaControls != null)
            {
                if (e.SourcePageType == typeof(SettingsPage))
                {
                    this.GlobalMediaControls.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.GlobalMediaControls.Visibility = Visibility.Visible;
                }
            }

            if (e.SourcePageType == typeof(SettingsPage))
            {
                RootNavigationView.SelectedItem = RootNavigationView.SettingsItem;
            }
            else if (e.SourcePageType != null)
            {
                var navigatedItem = RootNavigationView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(item => item.Tag is string tag && Type.GetType(tag) == e.SourcePageType);

                if (navigatedItem != null)
                {
                    RootNavigationView.SelectedItem = navigatedItem;
                }
            }
        }

        private void RootNavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        private void RootNavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            NavigationTransitionInfo transitionInfo = args.RecommendedNavigationTransitionInfo;

            if (args.IsSettingsInvoked)
            {
                NavigateToPage(typeof(SettingsPage), null, transitionInfo);
            }
            else if (args.InvokedItemContainer is NavigationViewItem selectedItem)
            {
                if (selectedItem.Tag is string pageTag)
                {
                    Type? targetPageType = Type.GetType(pageTag);
                    if (targetPageType != null)
                    {
                        NavigateToPage(targetPageType, null, transitionInfo);
                    }
                }
            }
        }

        private void NavigateToPage(Type targetPageType, object? parameter = null, NavigationTransitionInfo? transitionInfo = null)
        {
            if (ContentFrame.CurrentSourcePageType != targetPageType)
            {
                ContentFrame.Navigate(targetPageType, parameter, transitionInfo ?? new EntranceNavigationTransitionInfo());
            }
        }

        public void NavBar_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
            }
            else if (args.SelectedItemContainer is NavigationViewItem selectedItem)
            {
                string? selectedPage = selectedItem.Tag.ToString();

                switch (selectedPage)
                {
                    case "HomePage":
                        ContentFrame.Navigate(typeof(HomePage), null, new DrillInNavigationTransitionInfo());
                        break;
                    case "OnlinePage":
                        ContentFrame.Navigate(typeof(OnlinePage), null, new DrillInNavigationTransitionInfo());
                        break;
                    default:
                        break;
                }
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            GlobalMediaPlayerVM?.Cleanup();
            GlobalMediaControls?.Cleanup();
        }
    }
}