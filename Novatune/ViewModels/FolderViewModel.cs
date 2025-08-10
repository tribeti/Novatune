using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Novatune.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace Novatune.ViewModels
{
    public partial class FolderViewModel : ObservableObject
    {
        private static FolderViewModel? _instance;
        private static readonly object _lock = new object ();

        public static FolderViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new FolderViewModel ();
                        }
                    }
                }
                return _instance;
            }
        }

        private const string FolderTokensKey = "SavedFolderTokens";
        private const int DefaultBatchSize = 100;
        private const int ChannelCapacity = 1000;

        private readonly ApplicationDataContainer _localSettings;
        private CancellationTokenSource? _searchCancellationTokenSource;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly SemaphoreSlim _folderSemaphore;
        private readonly int _actualMaxConcurrentFolders;

        public ObservableCollection<StorageFolder> Folders { get; } = new ();
        public ObservableCollection<StorageFolder> SelectedFolders { get; } = new ();
        public ObservableCollection<LocalModel> Contents { get; } = new ();

        [ObservableProperty]
        public partial bool IsSearching { get; set; }

        [ObservableProperty]
        public partial string SearchStatus { get; set; }

        [ObservableProperty]
        public partial int FilesFoundCount { get; set; }

        [ObservableProperty]
        public partial int FoldersProcessedCount { get; set; }

        private readonly HashSet<string> _audioExtensions = new HashSet<string> (StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".aac", ".flac", ".wma", ".ogg", ".m4a", ".opus", ".m4b"
        };

        private readonly ConcurrentDictionary<string, DateTime> _folderScanCache = new ();
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes (5);

        public FolderViewModel ()
        {
            _actualMaxConcurrentFolders = Math.Max (2, Environment.ProcessorCount);
            _localSettings = ApplicationData.Current.LocalSettings;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread ();
            _folderSemaphore = new SemaphoreSlim (_actualMaxConcurrentFolders, _actualMaxConcurrentFolders);

            SelectedFolders.CollectionChanged += async (s, e) => await TriggerContentsUpdateAsync ();
            LoadSavedFoldersAsync ();
        }

        [RelayCommand (CanExecute = nameof (CanStartSearch))]
        public async Task StartSearchAsync ()
        {
            await TriggerContentsUpdateAsync ();
        }
        private bool CanStartSearch () => !IsSearching && SelectedFolders.Any ();

        public async Task ForceRefreshAsync ()
        {
            foreach (var folder in SelectedFolders)
            {
                _folderScanCache.TryRemove (folder.Path, out _);
            }
            await TriggerContentsUpdateAsync ();
        }

        [RelayCommand (CanExecute = nameof (CanCancelSearch))]
        public void CancelSearch ()
        {
            _searchCancellationTokenSource?.Cancel ();
            IsSearching = false;
            SearchStatus = "Tìm kiếm đã hủy.";
            StartSearchCommand.NotifyCanExecuteChanged ();
            CancelSearchCommand.NotifyCanExecuteChanged ();
        }
        private bool CanCancelSearch () => IsSearching;

        private async Task TriggerContentsUpdateAsync ()
        {
            _searchCancellationTokenSource?.Cancel ();
            _searchCancellationTokenSource = new CancellationTokenSource ();
            var token = _searchCancellationTokenSource.Token;

            IsSearching = true;
            SearchStatus = "Đang tìm kiếm...";
            FilesFoundCount = 0;
            FoldersProcessedCount = 0;
            Contents.Clear ();
            StartSearchCommand.NotifyCanExecuteChanged ();
            CancelSearchCommand.NotifyCanExecuteChanged ();

            if (!SelectedFolders.Any ())
            {
                SearchStatus = "Vui lòng chọn thư mục.";
                IsSearching = false;
                StartSearchCommand.NotifyCanExecuteChanged ();
                CancelSearchCommand.NotifyCanExecuteChanged ();
                return;
            }

            var foldersToSearch = SelectedFolders.ToList ();

            try
            {
                var channel = Channel.CreateBounded<LocalModel> (ChannelCapacity);
                var writer = channel.Writer;
                var reader = channel.Reader;
                var processingTask = ProcessSearchResultsAsync (reader, token);
                var searchTask = Task.Run (async () =>
                {
                    try
                    {
                        await Parallel.ForEachAsync (foldersToSearch,
                            new ParallelOptions
                            {
                                CancellationToken = token,
                                MaxDegreeOfParallelism = _actualMaxConcurrentFolders
                            },
                            async (folder, ct) =>
                            {
                                if (ct.IsCancellationRequested)
                                    return;
                                await SearchInFolderOptimizedAsync (folder, writer, ct);
                            });
                    }
                    finally
                    {
                        writer.Complete ();
                    }
                }, token);

                await Task.WhenAll (searchTask, processingTask);

                if (token.IsCancellationRequested)
                {
                    SearchStatus = $"Tìm kiếm đã hủy. Đã tìm thấy {FilesFoundCount} file trong {FoldersProcessedCount} thư mục.";
                }
                else
                {
                    SearchStatus = $"Hoàn tất. Tìm thấy {FilesFoundCount} file trong {FoldersProcessedCount} thư mục.";
                }
            }
            catch (OperationCanceledException)
            {
                SearchStatus = $"Tìm kiếm đã hủy. Đã tìm thấy {FilesFoundCount} file trong {FoldersProcessedCount} thư mục.";
            }
            catch (Exception ex)
            {
                SearchStatus = $"Lỗi: {ex.Message}";
                System.Diagnostics.Debug.WriteLine ($"Error during search: {ex}");
            }
            finally
            {
                IsSearching = false;
                _searchCancellationTokenSource?.Dispose ();
                _searchCancellationTokenSource = null;
                StartSearchCommand.NotifyCanExecuteChanged ();
                CancelSearchCommand.NotifyCanExecuteChanged ();
            }
        }

        private async Task ProcessSearchResultsAsync (ChannelReader<LocalModel> reader, CancellationToken token)
        {
            var batch = new List<LocalModel> (DefaultBatchSize);

            try
            {
                await foreach (var model in reader.ReadAllAsync (token))
                {
                    batch.Add (model);

                    if (batch.Count >= DefaultBatchSize)
                    {
                        await AddModelsToContentsOnUiThreadAsync (new List<LocalModel> (batch));
                        batch.Clear ();
                    }
                }
                if (batch.Count > 0)
                {
                    await AddModelsToContentsOnUiThreadAsync (batch);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        private async Task SearchInFolderOptimizedAsync (StorageFolder folder, ChannelWriter<LocalModel> writer, CancellationToken token)
        {
            await _folderSemaphore.WaitAsync (token);
            try
            {
                var folderPath = folder.Path;
                if (_folderScanCache.TryGetValue (folderPath, out var lastScan))
                {
                    if (DateTime.Now - lastScan < _cacheExpiry)
                    {
                        _dispatcherQueue.TryEnqueue (() =>
                        {
                            FoldersProcessedCount++;
                            SearchStatus = $"Bỏ qua thư mục đã scan gần đây: {folder.Name}";
                        });
                        return;
                    }
                }

                var foundFiles = await TryWindowsSearchAsync (folder, token);
                if (foundFiles?.Any () == true)
                {
                    await ProcessFoundFilesAsync (foundFiles, writer, token);
                    _folderScanCache[folderPath] = DateTime.Now;
                    _dispatcherQueue.TryEnqueue (() => FoldersProcessedCount++);
                    return;
                }

                await SearchInFolderRecursiveOptimizedAsync (folder, writer, token);
                _folderScanCache[folderPath] = DateTime.Now;
                _dispatcherQueue.TryEnqueue (() => FoldersProcessedCount++);
            }
            finally
            {
                _folderSemaphore.Release ();
            }
        }

        private async Task<IReadOnlyList<StorageFile>> TryWindowsSearchAsync (StorageFolder folder, CancellationToken token)
        {
            try
            {
                var queryOptions = new QueryOptions (CommonFileQuery.OrderByName, _audioExtensions.ToArray ())
                {
                    FolderDepth = FolderDepth.Deep,
                    IndexerOption = IndexerOption.UseIndexerWhenAvailable
                };

                var query = folder.CreateFileQueryWithOptions (queryOptions);
                return await query.GetFilesAsync ();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine ($"Windows Search failed for {folder.Path}: {ex.Message}");
                return null;
            }
        }

        private async Task ProcessFoundFilesAsync (IReadOnlyList<StorageFile> files, ChannelWriter<LocalModel> writer, CancellationToken token)
        {
            await Parallel.ForEachAsync (files,
                new ParallelOptions
                {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                },
                async (file, ct) =>
                {
                    if (ct.IsCancellationRequested)
                        return;

                    try
                    {
                        var audioModel = await LocalModel.FromStorageFileAsync (file);
                        if (audioModel != null)
                        {
                            await writer.WriteAsync (audioModel, ct);
                            _dispatcherQueue.TryEnqueue (() => FilesFoundCount++);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine ($"Error creating LocalModel for {file.Name}: {ex.Message}");
                    }
                });
        }

        private async Task SearchInFolderRecursiveOptimizedAsync (StorageFolder currentFolder, ChannelWriter<LocalModel> writer, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            IReadOnlyList<IStorageItem> items;
            try
            {
                items = await currentFolder.GetItemsAsync ();
            }
            catch (UnauthorizedAccessException ex)
            {
                _dispatcherQueue.TryEnqueue (() => SearchStatus = $"Bỏ qua thư mục không có quyền: {currentFolder.Name}");
                return;
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue (() => SearchStatus = $"Lỗi truy cập thư mục: {currentFolder.Name}");
                return;
            }

            if (items?.Count == 0)
                return;

            var files = items.OfType<StorageFile> ().Where (f => _audioExtensions.Contains (f.FileType.ToLowerInvariant ())).ToList ();
            var subFolders = items.OfType<StorageFolder> ().ToList ();
            if (files.Any ())
            {
                await ProcessFoundFilesAsync (files, writer, token);
            }
            foreach (var subFolder in subFolders)
            {
                if (token.IsCancellationRequested)
                    break;
                await SearchInFolderRecursiveOptimizedAsync (subFolder, writer, token);
            }
        }

        // TODO : optimize
        private async Task AddModelsToContentsOnUiThreadAsync (List<LocalModel> modelsToAdd)
        {
            await _dispatcherQueue.EnqueueAsync (() =>
            {
                foreach (var model in modelsToAdd)
                {
                    if (_searchCancellationTokenSource?.IsCancellationRequested != true)
                    {
                        Contents.Add (model);
                    }
                    else
                        break;
                }
                SearchStatus = $"Đang xử lý... {FilesFoundCount} file được tìm thấy trong {FoldersProcessedCount} thư mục.";
            });
        }

        public async Task LoadSpecificFolderAsync (StorageFolder folder)
        {
                _folderScanCache.TryRemove (folder.Path, out _);
            Contents.Clear ();
            SelectedFolders.Clear ();
            SelectedFolders.Add (folder);
            if (!Folders.Any (f => f.Path == folder.Path))
            {
                Folders.Insert (0, folder);
            }
            await TriggerContentsUpdateAsync ();
        }

        public void RemoveTemporaryFolder (StorageFolder folder)
        {
            var hasToken = StorageApplicationPermissions.FutureAccessList.Entries
                .Any (entry => entry.Metadata == folder.Path);

            if (!hasToken)
            {
                Folders.Remove (folder);
            }
            SelectedFolders.Remove (folder);
        }

        private async Task LoadSavedFoldersAsync ()
        {
            Folders.Clear ();
            var tokenList = _localSettings.Values[FolderTokensKey] as string;
            if (!string.IsNullOrEmpty (tokenList))
            {
                var tokens = tokenList.Split (',');
                var validTokens = new List<string> ();

                foreach (var token in tokens)
                {
                    try
                    {
                        if (StorageApplicationPermissions.FutureAccessList.ContainsItem (token))
                        {
                            var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync (token);
                            if (!Folders.Any (f => f.Path == folder.Path))
                            {
                                Folders.Add (folder);
                                validTokens.Add (token);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine ($"Failed to load folder for token {token}: {ex.Message}");
                    }
                }
                if (validTokens.Count != tokens.Length)
                {
                    _localSettings.Values[FolderTokensKey] = string.Join (",", validTokens);
                }
            }
        }

        [RelayCommand]
        public async Task AddFolderAsync ()
        {
            var picker = new FolderPicker ();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add ("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle (App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize (picker, hwnd);

            var folder = await picker.PickSingleFolderAsync ();
            if (folder != null && !Folders.Any (f => f.Path == folder.Path))
            {
                var token = StorageApplicationPermissions.FutureAccessList.Add (folder, folder.Path);
                Folders.Add (folder);
                SaveFolderTokens ();
                _folderScanCache.TryRemove (folder.Path, out _);
            }
        }

        [RelayCommand]
        public void RemoveFolder (StorageFolder folder)
        {
            if (folder is null)
                return;

            var tokenToRemove = StorageApplicationPermissions.FutureAccessList.Entries
                .Where (entry => entry.Metadata == folder.Path)
                .Select (entry => entry.Token)
                .FirstOrDefault ();

            if (!string.IsNullOrEmpty (tokenToRemove))
            {
                StorageApplicationPermissions.FutureAccessList.Remove (tokenToRemove);
            }

            Folders.Remove (folder);
            SelectedFolders.Remove (folder);
            _folderScanCache.TryRemove (folder.Path, out _);

            SaveFolderTokens ();
        }

        private void SaveFolderTokens ()
        {
            var tokens = StorageApplicationPermissions.FutureAccessList.Entries
                .Select (entry => entry.Token)
                .ToList ();

            _localSettings.Values[FolderTokensKey] = string.Join (",", tokens);
        }

        [RelayCommand]
        public void ClearCache ()
        {
            _folderScanCache.Clear ();
            SearchStatus = "Cache đã được xóa.";
        }

        public void Dispose ()
        {
            _searchCancellationTokenSource?.Cancel ();
            _searchCancellationTokenSource?.Dispose ();
            _folderSemaphore?.Dispose ();
        }
    }
}