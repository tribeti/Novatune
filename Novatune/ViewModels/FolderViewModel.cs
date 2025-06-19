using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Novatune.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace Novatune.ViewModels
{
    public partial class FolderViewModel : ObservableObject
    {
        private const string FolderTokensKey = "SavedFolderTokens";
        private readonly ApplicationDataContainer _localSettings;
        private CancellationTokenSource _searchCancellationTokenSource;
        private readonly DispatcherQueue _dispatcherQueue;

        public ObservableCollection<StorageFolder> Folders { get; } = new ObservableCollection<StorageFolder>();
        public ObservableCollection<StorageFolder> SelectedFolders { get; } = new ObservableCollection<StorageFolder>();
        public ObservableCollection<LocalModel> Contents { get; } = new ObservableCollection<LocalModel>();

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string _searchStatus;

        [ObservableProperty]
        private int _filesFoundCount;

        private readonly HashSet<string> _audioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".aac", ".flac", ".wma", ".ogg", ".m4a"
        };

        public FolderViewModel()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            SelectedFolders.CollectionChanged += async (s, e) => await TriggerContentsUpdateAsync();
            LoadSavedFoldersAsync();
        }

        [RelayCommand(CanExecute = nameof(CanStartSearch))]
        public async Task StartSearchAsync()
        {
            await TriggerContentsUpdateAsync();
        }
        private bool CanStartSearch() => !IsSearching && SelectedFolders.Any();

        [RelayCommand(CanExecute = nameof(CanCancelSearch))]
        public void CancelSearch()
        {
            _searchCancellationTokenSource?.Cancel();
            IsSearching = false;
            SearchStatus = "Tìm kiếm đã hủy.";
            StartSearchCommand.NotifyCanExecuteChanged();
            CancelSearchCommand.NotifyCanExecuteChanged();
        }
        private bool CanCancelSearch() => IsSearching;

        private async Task TriggerContentsUpdateAsync()
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();
            var token = _searchCancellationTokenSource.Token;

            IsSearching = true;
            SearchStatus = "Đang tìm kiếm...";
            FilesFoundCount = 0;
            Contents.Clear();
            StartSearchCommand.NotifyCanExecuteChanged();
            CancelSearchCommand.NotifyCanExecuteChanged();

            if (!SelectedFolders.Any())
            {
                SearchStatus = "Vui lòng chọn thư mục.";
                IsSearching = false;
                StartSearchCommand.NotifyCanExecuteChanged();
                CancelSearchCommand.NotifyCanExecuteChanged();
                return;
            }
            var foldersToSearch = SelectedFolders.ToList();

            try
            {
                await Task.Run(async () =>
                {
                    var tempFoundModels = new List<LocalModel>();
                    const int batchSize = 50;

                    foreach (var folder in foldersToSearch)
                    {
                        if (token.IsCancellationRequested) break;
                        await SearchInFolderRecursiveAsync(folder, tempFoundModels, token, batchSize);
                    }

                    if (tempFoundModels.Any() && !token.IsCancellationRequested)
                    {
                        AddModelsToContentsOnUiThread(tempFoundModels);
                        tempFoundModels.Clear();
                    }

                }, token);

                if (token.IsCancellationRequested)
                {
                    SearchStatus = $"Tìm kiếm đã hủy. Đã tìm thấy {FilesFoundCount} file.";
                }
                else
                {
                    SearchStatus = $"Hoàn tất. Tìm thấy {FilesFoundCount} file.";
                }
            }
            catch (OperationCanceledException)
            {
                SearchStatus = $"Tìm kiếm đã hủy. Đã tìm thấy {FilesFoundCount} file.";
            }
            catch (Exception ex)
            {
                SearchStatus = $"Lỗi: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error during search: {ex.ToString()}");
            }
            finally
            {
                IsSearching = false;
                _searchCancellationTokenSource?.Dispose();
                _searchCancellationTokenSource = null;
                StartSearchCommand.NotifyCanExecuteChanged();
                CancelSearchCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task SearchInFolderRecursiveAsync(StorageFolder currentFolder, List<LocalModel> batchList, CancellationToken token, int batchSize)
        {
            if (token.IsCancellationRequested) return;

            IReadOnlyList<IStorageItem> items = null;
            try
            {
                items = await currentFolder.GetItemsAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Access denied to folder: {currentFolder.Path}. Skipping. Error: {ex.Message}");
                _dispatcherQueue.TryEnqueue(() => SearchStatus = $"Bỏ qua thư mục không có quyền: {currentFolder.Name}");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error accessing folder: {currentFolder.Path}. Skipping. Error: {ex.Message}");
                _dispatcherQueue.TryEnqueue(() => SearchStatus = $"Lỗi truy cập thư mục: {currentFolder.Name}");
                return;
            }

            if (items == null) return;

            foreach (var item in items)
            {
                if (token.IsCancellationRequested) return;

                if (item is StorageFile file)
                {
                    if (_audioExtensions.Contains(file.FileType.ToLowerInvariant()))
                    {
                        try
                        {
                            var audioModel = await LocalModel.FromStorageFileAsync(file);
                            if (audioModel != null)
                            {
                                lock (batchList)
                                    batchList.Add(audioModel);
                                _dispatcherQueue.TryEnqueue(() => FilesFoundCount++);

                                if (batchList.Count >= batchSize)
                                {
                                    AddModelsToContentsOnUiThread(new List<LocalModel>(batchList));
                                    batchList.Clear();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating LocalAudioModel for {file.Name}: {ex.Message}");
                        }
                    }
                }
                else if (item is StorageFolder subFolder)
                {
                    await SearchInFolderRecursiveAsync(subFolder, batchList, token, batchSize);
                }
            }
        }

        private void AddModelsToContentsOnUiThread(List<LocalModel> modelsToAdd)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var model in modelsToAdd)
                {
                    if (!_searchCancellationTokenSource.IsCancellationRequested)
                    {
                        Contents.Add(model);
                    }
                    else break;
                }
                SearchStatus = $"Đang xử lý... {FilesFoundCount} file được tìm thấy.";
            });
        }

        private async Task LoadSavedFoldersAsync()
        {
            Folders.Clear();
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
                SaveFolderTokens();
            }
        }

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

        [RelayCommand]
        public void RemoveFolder(StorageFolder folder)
        {
            if (folder == null) return;

            var tokenToRemove = StorageApplicationPermissions.FutureAccessList.Entries
                .Where(entry => entry.Metadata == folder.Path)
                .Select(entry => entry.Token)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(tokenToRemove))
            {
                StorageApplicationPermissions.FutureAccessList.Remove(tokenToRemove);
            }

            Folders.Remove(folder);
        }

        private void SaveFolderTokens()
        {
            var tokens = StorageApplicationPermissions.FutureAccessList.Entries
                .Select(entry => entry.Token)
                .ToList();

            _localSettings.Values[FolderTokensKey] = string.Join(",", tokens);
        }
    }
}
