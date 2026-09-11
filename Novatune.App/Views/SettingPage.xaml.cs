using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Services;
using Novatune.App.ViewModels;

namespace Novatune.App.Views;

public sealed partial class SettingPage : Page
{
    private readonly SettingsService _settingsService;
    private readonly MediaViewModel _mediaViewModel;

    public SettingPage()
    {
        _settingsService = App.Current.Services.GetService<SettingsService>()!;
        _mediaViewModel = App.Current.Services.GetService<MediaViewModel>()!;
        InitializeComponent();
        TraySwitch.IsOn = _settingsService.Settings.MinimizeOnClose;
        RPCSwitch.IsOn = _settingsService.Settings.EnableDiscordRpc;
    }

    private void TraySwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (this.IsLoaded)
        {
            _settingsService.Settings.MinimizeOnClose = TraySwitch.IsOn;
            _settingsService.Save();
        }
    }

    private void RPCSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (this.IsLoaded)
        {
            _settingsService.Settings.EnableDiscordRpc = RPCSwitch.IsOn;
            _settingsService.Save();
            _mediaViewModel.UpdateDiscordPresence();
        }
    }
}
