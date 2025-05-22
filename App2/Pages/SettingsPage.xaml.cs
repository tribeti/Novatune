using App2.Controls;
using App2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace App2.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            LoadAutoHideToggleSwitchState();
        }

        private void LoadAutoHideToggleSwitchState()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(MediaControlsView.AutoHideSettingKey, out object settingValue) &&
                settingValue is bool isAutoHideEnabled)
            {
                AutoHideToggleSwitch.IsOn = isAutoHideEnabled;
            }
            else
            {
                bool defaultAutoHideState = true;
                AutoHideToggleSwitch.IsOn = defaultAutoHideState;
                localSettings.Values[MediaControlsView.AutoHideSettingKey] = defaultAutoHideState;
            }
        }

        private void AutoHideToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                bool isAutoHideNowEnabled = toggleSwitch.IsOn;
                ApplicationData.Current.LocalSettings.Values[MediaControlsView.AutoHideSettingKey] = isAutoHideNowEnabled;
                var mainWindow = App.MainWindow as MainWindow;
                if (mainWindow != null && mainWindow.GlobalMediaControlsPublic != null)
                {
                    mainWindow.GlobalMediaControlsPublic.UpdateAutoHideFeatureState(isAutoHideNowEnabled);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Error: Could not access GlobalMediaControls from SettingsPage to update auto-hide state.");
                }
            }
        }
    }
}