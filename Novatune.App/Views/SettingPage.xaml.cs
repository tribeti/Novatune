using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Services;

namespace Novatune.App.Views;

public sealed partial class SettingPage : Page
{
    private readonly SettingsService _settingsService;
    public SettingPage()
    {
        _settingsService = App.Current.Services.GetService<SettingsService>()!;
        InitializeComponent();
        TraySwitch.IsOn = _settingsService.Settings.MinimizeOnClose;
    }

    private void TraySwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (this.IsLoaded)
        {
            _settingsService.Settings.MinimizeOnClose = TraySwitch.IsOn;
            _settingsService.Save();
        }
    }
}
