using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Novatune.App.Services;

public sealed class DialogService
{
    private static XamlRoot Root => App.Current.MainWindow!.Content.XamlRoot;

    public static async Task ShowErrorAsync(string message, string? title = null)
    {
        var dialog = new ContentDialog
        {
            Title = title ?? Strings.Get("Dialog_Error"),
            Content = message,
            CloseButtonText = Strings.Get("Dialog_Ok"),
            XamlRoot = Root,
        };
        await dialog.ShowAsync();
    }

    public static async Task<bool> ShowConfirmAsync(string title, string message, string primaryText, string closeText)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Root,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public static async Task<string?> ShowInputAsync(string title, string placeholder, string primaryText, string closeText)
    {
        var inputBox = new TextBox
        {
            PlaceholderText = placeholder,
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = inputBox,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Root,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return null;

        var text = inputBox.Text?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public static LoadingDialog ShowLoading(string title)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new ProgressRing { IsActive = true, Width = 48, Height = 48 },
            XamlRoot = Root,
        };
        return new LoadingDialog(dialog, dialog.ShowAsync());
    }
}

public sealed class LoadingDialog(ContentDialog dialog, IAsyncOperation<ContentDialogResult> operation)
{
    public async Task HideAsync()
    {
        dialog.Hide();
        await operation;
    }
}
