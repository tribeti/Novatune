using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Novatune.App.ViewModels;
using System;

namespace Novatune.App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
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
        _window = new MainWindow();
        _window.Activate();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MediaViewModel>();
        return services.BuildServiceProvider();
    }
}
