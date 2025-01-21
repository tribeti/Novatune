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


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App2.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
                // Get the saved folder tokens
                var tokenList = localSettings.Values[FolderTokensKey] as string;
                if (!string.IsNullOrEmpty(tokenList))
                {
                    var tokens = tokenList.Split(',');
                    foreach (var token in tokens)
                    {
                        try
                        {
                            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
                            {
                                var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                                StorageFolders.Add(folder);
                            }
                        }
                        catch (Exception) { } // Skip if folder is no longer accessible
                    }
                }
            }
            catch (Exception) { } // Handle any potential errors during loading
        }

        private void SaveFolderTokens()
        {
            try
            {
                var tokens = new List<string>();
                foreach (var item in StorageApplicationPermissions.FutureAccessList.Entries)
                {
                    tokens.Add(item.Token);
                }

                // Save the tokens as a comma-separated string
                var tokenString = string.Join(",", tokens);
                localSettings.Values[FolderTokensKey] = tokenString;
            }
            catch (Exception) { }
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var senderButton = sender as Button;
            senderButton.IsEnabled = false;
            
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            openPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            
            if (folder != null)
            {
                bool folderExists = false;
                foreach (var existingFolder in StorageFolders)
                {
                    if (existingFolder.Path == folder.Path)
                    {
                        folderExists = true;
                        break;
                    }
                }

                if (!folderExists)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace("Folder_" + Guid.NewGuid().ToString(), folder);
                    StorageFolders.Add(folder);
                    SaveFolderTokens(); // Save whenever we add a new folder
                }
            }

            senderButton.IsEnabled = true;
        }

        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var folderPath = button.Tag as string;

            // Find and remove the folder
            StorageFolder folderToRemove = null;
            foreach (var folder in StorageFolders)
            {
                if (folder.Path == folderPath)
                {
                    folderToRemove = folder;
                    break;
                }
            }

            if (folderToRemove != null)
            {
                StorageFolders.Remove(folderToRemove);

                // Remove from FutureAccessList and save
                foreach (var entry in StorageApplicationPermissions.FutureAccessList.Entries)
                {
                    try
                    {
                        var folder = StorageApplicationPermissions.FutureAccessList.GetFolderAsync(entry.Token).GetAwaiter().GetResult();
                        if (folder.Path == folderPath)
                        {
                            StorageApplicationPermissions.FutureAccessList.Remove(entry.Token);
                            break;
                        }
                    }
                    catch { }
                }

                SaveFolderTokens();
            }
        }
    }
}
