using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;

// media namespace
using Windows.Media.Core;
using Windows.Media.Playback;

using App2.Pages;
using App2.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI;
using Microsoft.UI.Windowing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App2
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();


            ViewStorage = Ioc.Default.GetService<FolderViewModel>();

            ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;
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
                    default:
                        break;
                }
            }
        }
    }
}

// min size 660x360
