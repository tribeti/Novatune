using DevWinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Novatune.App.ViewModels;
using Novatune.App.Views;
using System;
using System.Linq;

namespace Novatune.App;

public sealed partial class MainWindow : Window
{
    public MediaViewModel ViewModel { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        this.Media_Timeline.Loaded += Media_Timeline_Loaded;
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
}
