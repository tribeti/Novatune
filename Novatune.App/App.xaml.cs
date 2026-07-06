using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Novatune.App.ViewModels;
using System;
using WinUIEx;

namespace Novatune.App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public Window? MainWindow { get; private set; }
    public IServiceProvider Services { get; }
    public new static App Current => (App) Application.Current;
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        var wm = WindowManager.Get(MainWindow);
        wm.WindowStateChanged += (s, state) => wm.AppWindow.IsShownInSwitchers = state != WindowState.Minimized;

        bool launchToTray = false;

        if (launchToTray)
            wm.WindowState = WindowState.Minimized;
        else
            MainWindow.Activate();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MediaViewModel>();
        services.AddSingleton<RadioViewModel>();
        return services.BuildServiceProvider();
    }
}
