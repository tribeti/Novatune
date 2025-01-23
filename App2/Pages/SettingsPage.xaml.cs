using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace App2.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<StorageFolder> StorageFolders { get; } = new ObservableCollection<StorageFolder>();
        private const string FolderTokensKey = "SavedFolderTokens";
        private ApplicationDataContainer localSettings;

        public SettingsPage()
        {
            this.InitializeComponent();
            localSettings = ApplicationData.Current.LocalSettings;
            LoadSavedFolders();
        }

        private async void LoadSavedFolders()
        {
            try
            {
                var tokenList = localSettings.Values[FolderTokensKey] as string;
                if (!string.IsNullOrEmpty(tokenList))
                {
                    var tokens = tokenList.Split(',');
                    foreach (var token in tokens)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(token) &&
                                StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
                            {
                                var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);

                                if (!StorageFolders.Any(f => f.Path == folder.Path))
                                {
                                    StorageFolders.Add(folder);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error loading folder: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle specific exceptions if needed
                System.Diagnostics.Debug.WriteLine($"Error in LoadSavedFolders: {ex.Message}");
            }
        }

        private void SaveFolderTokens()
        {
            try
            {
                var tokens = StorageApplicationPermissions.FutureAccessList.Entries
                    .Select(entry => entry.Token)
                    .ToList();

                var tokenString = string.Join(",", tokens);
                localSettings.Values[FolderTokensKey] = tokenString;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveFolderTokens: {ex.Message}");
            }
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PickFolderButton.IsEnabled = false;

                FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();
                var window = App.MainWindow;
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                openPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                openPicker.FileTypeFilter.Add("*");

                StorageFolder folder = await openPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    if (!StorageFolders.Any(f => f.Path == folder.Path))
                    {
                        string token = StorageApplicationPermissions.FutureAccessList.Add(folder, folder.Path);
                        StorageFolders.Add(folder);

                        SaveFolderTokens();
                    }
                    else
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Duplicate Folder",
                            Content = "This folder is already in your library.",
                            CloseButtonText = "OK"
                        };

                        dialog.XamlRoot = this.XamlRoot;
                        await dialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PickFolderButton_Click: {ex.Message}");
            }
            finally
            {
                PickFolderButton.IsEnabled = true;
            }
        }


        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var folderPath = button?.Tag as string;

                if (string.IsNullOrWhiteSpace(folderPath))
                    return;

                var folderToRemove = StorageFolders.FirstOrDefault(f => f.Path == folderPath);
                if (folderToRemove != null)
                {
                    StorageFolders.Remove(folderToRemove);

                    var tokenToRemove = StorageApplicationPermissions.FutureAccessList.Entries
                        .FirstOrDefault(entry =>
                        {
                            try
                            {
                                var folder = StorageApplicationPermissions.FutureAccessList.GetFolderAsync(entry.Token).GetAwaiter().GetResult();
                                return folder.Path == folderPath;
                            }
                            catch
                            {
                                return false;
                            }
                        });

                    if (tokenToRemove != null)
                    {
                        StorageApplicationPermissions.FutureAccessList.Remove(tokenToRemove.Token);
                    }

                    SaveFolderTokens();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RemoveFolderButton_Click: {ex.Message}");
            }
        }
    }
}
