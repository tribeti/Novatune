using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using App2.Pages;
using App2.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinUIEx;
using Microsoft.UI.Composition.SystemBackdrops;

namespace App2
{
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        public MediaPlayerViewModel GlobalMediaPlayerVM { get; private set; }
        public MainWindow()
        {
            this.InitializeComponent();

            this.MinHeight = 500;
            this.MinWidth = 700;

            this.PersistenceId = "MainWindow";

            this.Closed += MainWindow_Closed;
            ViewStorage = Ioc.Default.GetService<FolderViewModel>();

            ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;



            GlobalMediaPlayerVM = new MediaPlayerViewModel();

            GlobalMediaControls.Initialize(GlobalMediaPlayerVM);

            ContentFrame.Navigate(typeof(HomePage), null, new DrillInNavigationTransitionInfo());
            NavBar.SelectedItem = NavBar.MenuItems.FirstOrDefault();
        }


        public FolderViewModel? ViewStorage { get;}

        public void NavBar_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
            }
            else if (args.SelectedItemContainer is NavigationViewItem selectedItem)
            {
                string selectedPage = selectedItem.Tag.ToString();

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