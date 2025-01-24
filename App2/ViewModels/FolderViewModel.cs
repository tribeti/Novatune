using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace App2.ViewModels
{
    public partial class FolderViewModel : ObservableObject
    {
        private const string FolderTokensKey = "SavedFolderTokens";
        private readonly ApplicationDataContainer _localSettings;

        public ObservableCollection<StorageFolder> Folders { get; } = new ObservableCollection<StorageFolder>();

        public ObservableCollection<IStorageItem> Contents { get; } = new ObservableCollection<IStorageItem>();

        [ObservableProperty]
        private StorageFolder selectedFolder;

        public FolderViewModel()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
            LoadSavedFoldersAsync();
        }

        [RelayCommand]
        public void SelectFolder(StorageFolder folder)
        {
            SelectedFolder = folder;
        }

        partial void OnSelectedFolderChanged(StorageFolder value)
        {
            if (value != null)
            {
                LoadFolderContentsAsync(value);
            }
        }

        private async Task LoadFolderContentsAsync(StorageFolder folder)
        {
            Contents.Clear();
            var items = await folder.GetItemsAsync();
            foreach (var item in items)
            {
                Contents.Add(item);
            }
        }
        
        private async Task LoadSavedFoldersAsync()
        {
            var tokenList = _localSettings.Values[FolderTokensKey] as string;
            if (!string.IsNullOrEmpty(tokenList))
            {
                var tokens = tokenList.Split(',');
                foreach (var token in tokens)
                {
                    if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
                    {
                        var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                        if (!Folders.Any(f => f.Path == folder.Path))
                        {
                            Folders.Add(folder);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///    Adds a folder to the list of folders to display.
        /// </summary>
        [RelayCommand]
        public async Task AddFolderAsync()
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null && !Folders.Any(f => f.Path == folder.Path))
            {
                var token = StorageApplicationPermissions.FutureAccessList.Add(folder, folder.Path);
                Folders.Add(folder);
                SaveFolderTokens();
            }
        }

        /// <summary>
        ///     Removes a folder from the list of folders to display.
        /// </summary>
        [RelayCommand]
        public void RemoveFolder(StorageFolder folder)
        {
            if (folder == null) return;

            StorageApplicationPermissions.FutureAccessList.Remove(
                StorageApplicationPermissions.FutureAccessList.Entries
                    .Where(entry => entry.Metadata == folder.Path)
                    .Select(entry => entry.Token)
                    .FirstOrDefault()
            );

            Folders.Remove(folder);
        }

        /// <summary>
        /// saves the folder tokens to the local settings.
        /// </summary>
        private void SaveFolderTokens()
        {
            var tokens = StorageApplicationPermissions.FutureAccessList.Entries
                .Select(entry => entry.Token)
                .ToList();

            _localSettings.Values[FolderTokensKey] = string.Join(",", tokens);
        }
    }
}
