using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Novatune.ViewModels;

namespace Novatune;

public partial class App : Application
{
    public static MainWindow MainWindow = new();
    public App ()
    {
        InitializeComponent();

        Ioc.Default.ConfigureServices(new ServiceCollection()
            .AddSingleton<FolderViewModel>()
            .BuildServiceProvider());

    }

    protected override void OnLaunched (LaunchActivatedEventArgs args)
    {
        MainWindow.Activate();
    }
}