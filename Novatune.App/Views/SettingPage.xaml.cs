using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Novatune.App.Services;
using Novatune.App.ViewModels;
using System;

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
        ThemeSelector.SelectedIndex = (int) _settingsService.Settings.Theme;
        AccentSelector.SelectedIndex = (int) _settingsService.Settings.AccentColor;
        LanguageSelector.SelectedIndex = _settingsService.Settings.Language == AppSettings.DefaultLanguage ? 0 : 1;
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

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.IsLoaded)
        {
            var theme = (AppTheme) ThemeSelector.SelectedIndex;
            _settingsService.Settings.Theme = theme;
            _settingsService.Save();
            (App.Current.MainWindow as MainWindow)?.ApplyTheme(theme);
        }
    }

    private void AccentSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.IsLoaded)
        {
            var accent = (AppAccentColor) AccentSelector.SelectedIndex;
            _settingsService.Settings.AccentColor = accent;
            _settingsService.Save();
            (App.Current.MainWindow as MainWindow)?.ApplyAccentColor(accent);
        }
    }

    private async void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!this.IsLoaded)
            return;

        var language = LanguageSelector.SelectedIndex == 1 ? "vi-VN" : AppSettings.DefaultLanguage;
        if (language == _settingsService.Settings.Language)
            return;

        // restarting tears down the current playback session - ask before losing it
        if (_mediaViewModel.IsPlaying || _mediaViewModel.Playlist.Count > 0)
        {
            var confirmed = await DialogService.ShowConfirmAsync(
                Strings.Get("Dialog_RestartTitle"),
                Strings.Get("Dialog_RestartConfirm"),
                Strings.Get("Dialog_Restart"),
                Strings.Get("Dialog_Cancel"));
            if (!confirmed)
            {
                RestoreLanguageSelection();
                return;
            }
        }

        var previousLanguage = _settingsService.Settings.Language;
        _settingsService.Settings.Language = language;
        if (!_settingsService.Save())
        {
            _settingsService.Settings.Language = previousLanguage;
            RestoreLanguageSelection();
            await DialogService.ShowErrorAsync(Strings.Get("Dialog_SaveFailed"));
            return;
        }

        Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = language;
        Strings.Reset();
        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        _mediaViewModel.UpdateDiscordPresence();
    }

    private void RestoreLanguageSelection()
        => LanguageSelector.SelectedIndex = _settingsService.Settings.Language == AppSettings.DefaultLanguage ? 0 : 1;
}
