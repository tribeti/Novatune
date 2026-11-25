using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Novatune.App.Services;

public sealed class DialogService
{
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static XamlRoot Root => App.Current.MainWindow!.Content.XamlRoot;

    public static async Task ShowErrorAsync(string message, string? title = null)
    {
        await ShowAsync(() => new ContentDialog
        {
            Title = title ?? Strings.Get("Dialog_Error"),
            Content = message,
            CloseButtonText = Strings.Get("Dialog_Ok"),
            XamlRoot = Root,
        });
    }

    public static async Task<bool> ShowConfirmAsync(string title, string message, string primaryText, string closeText)
    {
        var result = await ShowAsync(() => new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Root,
        });
        return result == ContentDialogResult.Primary;
    }

    public static async Task<string?> ShowInputAsync(string title, string placeholder, string primaryText, string closeText)
    {
        var inputBox = new TextBox
        {
            PlaceholderText = placeholder,
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
        };

        var result = await ShowAsync(() => new ContentDialog
        {
            Title = title,
            Content = inputBox,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Root,
        });

        if (result != ContentDialogResult.Primary)
            return null;

        var text = inputBox.Text?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public static async Task<LoadingDialog> ShowLoadingAsync(string title)
    {
        await _gate.WaitAsync();
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new ProgressRing { IsActive = true, Width = 48, Height = 48 },
                XamlRoot = Root,
            };
            return new LoadingDialog(dialog, dialog.ShowAsync());
        }
        catch
        {
            _gate.Release();
            throw;
        }
    }

    private static async Task<ContentDialogResult> ShowAsync(Func<ContentDialog> create)
    {
        await _gate.WaitAsync();
        try
        {
            var dialog = create();
            return await dialog.ShowAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    public sealed class LoadingDialog(ContentDialog dialog, IAsyncOperation<ContentDialogResult> operation)
    {
        private bool _closed;

        public async Task HideAsync()
        {
            if (_closed)
                return;

            _closed = true;
            dialog.Hide();
            try
            {
                await operation;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
