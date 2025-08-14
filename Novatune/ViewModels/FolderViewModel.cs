using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Novatune.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private static readonly object _lock = new();

        public static FolderViewModel Instance
        {
            get
            {
                if ( _instance == null )
                {
                    lock ( _lock )
                    {
                        if ( _instance == null )
                        {
                            _instance = new FolderViewModel();
                        }
                    }
                }
                return _instance;
            }
        }

        private const string FolderTokensKey = "DkudeUjO2bpHr1jua5WA";
        private const int DefaultBatchSize = 100;
        private const int ChannelCapacity = 1000;

        private readonly ApplicationDataContainer _localSettings;
        private CancellationTokenSource? _searchCancellationTokenSource;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly SemaphoreSlim _folderSemaphore;
        private readonly int _actualMaxConcurrentFolders;
        private readonly SemaphoreSlim _searchSemaphore = new(1 , 1);

        public ObservableCollection<StorageFolder> Folders { get; } = new();
        public ObservableCollection<StorageFolder> SelectedFolders { get; } = new();
        public ObservableCollection<LocalFilesModel> Contents { get; } = new();

        [ObservableProperty]
        public partial bool IsSearching { get; set; }

        [ObservableProperty]
        public partial string SearchStatus { get; set; }

        private readonly HashSet<string> _audioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".aac", ".flac", ".wma", ".ogg", ".m4a", ".opus", ".m4b"
        };

        private readonly ConcurrentDictionary<string , DateTime> _folderScanCache = new();
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public FolderViewModel ()
        {
            _actualMaxConcurrentFolders = Math.Max(2 , Environment.ProcessorCount);
            _localSettings = ApplicationData.Current.LocalSettings;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _folderSemaphore = new SemaphoreSlim(_actualMaxConcurrentFolders , _actualMaxConcurrentFolders);
            SearchStatus = "Sẵn sàng tìm kiếm";

            SelectedFolders.CollectionChanged += async (s , e) => await TriggerContentsUpdateAsync();
            LoadSavedFoldersAsync();
        }

        [RelayCommand(CanExecute = nameof(CanStartSearch))]
        public async Task StartSearchAsync ()
        {
            await TriggerContentsUpdateAsync();
        }
        private bool CanStartSearch () => !IsSearching && SelectedFolders.Any();

        public async Task ForceRefreshAsync ()
        {
            foreach ( var folder in SelectedFolders )
            {
                _folderScanCache.TryRemove(folder.Path , out _);
            }
            await TriggerContentsUpdateAsync();
        }

        [RelayCommand(CanExecute = nameof(CanCancelSearch))]
        public void CancelSearch ()
        {
            _searchCancellationTokenSource?.Cancel();
            IsSearching = false;
            SearchStatus = "Tìm kiếm đã hủy.";
            StartSearchCommand.NotifyCanExecuteChanged();
            CancelSearchCommand.NotifyCanExecuteChanged();
        }
        private bool CanCancelSearch () => IsSearching;

        private async Task TriggerContentsUpdateAsync ()
        {
            if ( !await _searchSemaphore.WaitAsync(100) )
            {
                return;
            }

            try
            {
                _searchCancellationTokenSource?.Cancel();
                _searchCancellationTokenSource?.Dispose();

                await Task.Delay(50);

                _searchCancellationTokenSource = new CancellationTokenSource();
                var token = _searchCancellationTokenSource.Token;

                IsSearching = true;
                SearchStatus = "Đang tìm kiếm...";
                Contents.Clear();
                StartSearchCommand.NotifyCanExecuteChanged();
                CancelSearchCommand.NotifyCanExecuteChanged();

                if ( !SelectedFolders.Any() )
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
                    var channel = Channel.CreateBounded<LocalFilesModel>(ChannelCapacity);
                    var writer = channel.Writer;
                    var reader = channel.Reader;

                    var processingTask = ProcessSearchResultsAsync(reader , token);
                    var searchTask = Task.Run(async () =>
                    {
                        try
                        {
                            await Parallel.ForEachAsync(foldersToSearch ,
                                new ParallelOptions
                                {
                                    CancellationToken = token ,
                                    MaxDegreeOfParallelism = _actualMaxConcurrentFolders
                                } ,
                                async (folder , ct) =>
                                {
                                    if ( ct.IsCancellationRequested )
                                        return;
                                    await SearchInFolderOptimizedAsync(folder , writer , ct);
                                });
                        }
                        catch ( OperationCanceledException )
                        {
                        }
                        finally
                        {
                            writer.Complete();
                        }
                    } , token);

                    await Task.WhenAll(searchTask , processingTask);

                    if ( !token.IsCancellationRequested )
                    {
                        var totalFound = Contents.Count;
                        SearchStatus = $"Hoàn thành. Tìm thấy {totalFound} tệp âm thanh.";
                    }
                }
                catch ( OperationCanceledException )
                {
                    if ( !token.IsCancellationRequested )
                    {
                        SearchStatus = "Tìm kiếm đã bị hủy.";
                    }
                }
                catch ( Exception ex )
                {
                    SearchStatus = $"Lỗi: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"Error during search: {ex}");
                }
            }
            finally
            {
                IsSearching = false;
                StartSearchCommand.NotifyCanExecuteChanged();
                CancelSearchCommand.NotifyCanExecuteChanged();
                _searchSemaphore.Release();
            }
        }

        private async Task ProcessSearchResultsAsync (ChannelReader<LocalFilesModel> reader , CancellationToken token)
        {
            var batch = new List<LocalFilesModel>(DefaultBatchSize);
            int processedCount = 0;

            try
            {
                await foreach ( var model in reader.ReadAllAsync(token) )
                {
                    batch.Add(model);
                    processedCount++;

                    if ( batch.Count >= DefaultBatchSize )
                    {
                        await AddModelsToContentsOnUiThreadAsync(new List<LocalFilesModel>(batch));
                        batch.Clear();

                        if ( processedCount % ( DefaultBatchSize * 2 ) == 0 )
                        {
                            _dispatcherQueue.TryEnqueue(() =>
                            {
                                SearchStatus = $"Đang xử lý... Đã tìm thấy {processedCount} tệp.";
                            });
                        }
                    }
                }

                if ( batch.Count > 0 )
                {
                    await AddModelsToContentsOnUiThreadAsync(batch);
                }
            }
            catch ( OperationCanceledException )
            {
                // Expected when cancellation is requested
                System.Diagnostics.Debug.WriteLine("ProcessSearchResultsAsync was cancelled");
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine($"Error in ProcessSearchResultsAsync: {ex}");
            }
        }

        private async Task SearchInFolderOptimizedAsync (StorageFolder folder , ChannelWriter<LocalFilesModel> writer , CancellationToken token)
        {
            if ( token.IsCancellationRequested )
                return;

            await _folderSemaphore.WaitAsync(token);
            try
            {
                var folderPath = folder.Path;
                if ( _folderScanCache.TryGetValue(folderPath , out var lastScan) )
                {
                    if ( DateTime.Now - lastScan < _cacheExpiry )
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            SearchStatus = $"Bỏ qua thư mục đã scan gần đây: {folder.Name}";
                        });
                        return;
                    }
                }

                _dispatcherQueue.TryEnqueue(() =>
                {
                    SearchStatus = $"Đang quét: {folder.Name}";
                });

                var foundFiles = await TryWindowsSearchAsync(folder , token);
                if ( foundFiles?.Any() == true )
                {
                    await ProcessFoundFilesAsync(foundFiles , writer , token);
                    _folderScanCache [folderPath] = DateTime.Now;
                    return;
                }

                await SearchInFolderRecursiveOptimizedAsync(folder , writer , token);
                _folderScanCache [folderPath] = DateTime.Now;
            }
            catch ( OperationCanceledException ) { }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine($"Error searching folder {folder.Name}: {ex}");
                _dispatcherQueue.TryEnqueue(() =>
                {
                    SearchStatus = $"Lỗi khi quét thư mục: {folder.Name}";
                });
            }
            finally
            {
                _folderSemaphore.Release();
            }
        }

        private async Task<IReadOnlyList<StorageFile>?> TryWindowsSearchAsync (StorageFolder folder , CancellationToken token)
        {
            try
            {
                var queryOptions = new QueryOptions(CommonFileQuery.OrderByName , _audioExtensions.ToArray())
                {
                    FolderDepth = FolderDepth.Deep ,
                    IndexerOption = IndexerOption.UseIndexerWhenAvailable
                };

                var query = folder.CreateFileQueryWithOptions(queryOptions);
                return await query.GetFilesAsync();
            }
            catch ( OperationCanceledException )
            {
                throw; // Re-throw cancellation
            }
            catch ( Exception )
            {
                return null;
            }
        }

        private static async Task ProcessFoundFilesAsync (IReadOnlyList<StorageFile> files , ChannelWriter<LocalFilesModel> writer , CancellationToken token)
        {
            await Parallel.ForEachAsync(files ,
                new ParallelOptions
                {
                    CancellationToken = token ,
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                } ,
                async (file , ct) =>
                {
                    if ( ct.IsCancellationRequested )
                        return;

                    try
                    {
                        var audioModel = await LocalFilesModel.FromStorageFileAsync(file);
                        if ( audioModel != null )
                        {
                            await writer.WriteAsync(audioModel , ct);
                        }
                    }
                    catch ( OperationCanceledException ) { }
                    catch ( Exception ex )
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating LocalModel for {file.Name}: {ex.Message}");
                    }
                });
        }

        private async Task SearchInFolderRecursiveOptimizedAsync (StorageFolder currentFolder , ChannelWriter<LocalFilesModel> writer , CancellationToken token)
        {
            if ( token.IsCancellationRequested )
                return;

            IReadOnlyList<IStorageItem> items;
            try
            {
                items = await currentFolder.GetItemsAsync();
            }
            catch ( UnauthorizedAccessException )
            {
                _dispatcherQueue.TryEnqueue(() => SearchStatus = $"Bỏ qua thư mục không có quyền: {currentFolder.Name}");
                return;
            }
            catch ( OperationCanceledException )
            {
                return;
            }
            catch ( Exception )
            {
                _dispatcherQueue.TryEnqueue(() => SearchStatus = $"Lỗi truy cập thư mục: {currentFolder.Name}");
                return;
            }

            if ( items?.Count == 0 )
                return;

            var files = items.OfType<StorageFile>().Where(f => _audioExtensions.Contains(f.FileType.ToLowerInvariant())).ToList();
            var subFolders = items.OfType<StorageFolder>().ToList();

            if ( files.Any() )
            {
                await ProcessFoundFilesAsync(files , writer , token);
            }

            foreach ( var subFolder in subFolders )
            {
                if ( token.IsCancellationRequested )
                    break;
                await SearchInFolderRecursiveOptimizedAsync(subFolder , writer , token);
            }
        }

        // TODO : optimize
        private async Task AddModelsToContentsOnUiThreadAsync (List<LocalFilesModel> modelsToAdd)
        {
            try
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    foreach ( var model in modelsToAdd )
                    {
                        if ( _searchCancellationTokenSource?.IsCancellationRequested != true )
                        {
                            Contents.Add(model);
                        }
                        else
                            break;
                    }
                });
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine($"Error adding models to UI: {ex}");
            }
        }

        public async Task LoadSpecificFolderAsync (StorageFolder folder)
        {
            _folderScanCache.TryRemove(folder.Path , out _);
            Contents.Clear();
            SelectedFolders.Clear();
            SelectedFolders.Add(folder);
            if ( !Folders.Any(f => f.Path == folder.Path) )
            {
                Folders.Insert(0 , folder);
            }
            await TriggerContentsUpdateAsync();
        }

        public void RemoveTemporaryFolder (StorageFolder folder)
        {
            var hasToken = StorageApplicationPermissions.FutureAccessList.Entries
                .Any(entry => entry.Metadata == folder.Path);

            if ( !hasToken )
            {
                Folders.Remove(folder);
            }
            SelectedFolders.Remove(folder);
        }

        private async Task LoadSavedFoldersAsync ()
        {
            try
            {
                Folders.Clear();
                var tokenList = _localSettings.Values [FolderTokensKey] as string;
                if ( !string.IsNullOrEmpty(tokenList) )
                {
                    var tokens = tokenList.Split(',');
                    var validTokens = new List<string>();

                    foreach ( var token in tokens )
                    {
                        try
                        {
                            if ( StorageApplicationPermissions.FutureAccessList.ContainsItem(token) )
                            {
                                var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                                if ( !Folders.Any(f => f.Path == folder.Path) )
                                {
                                    Folders.Add(folder);
                                    validTokens.Add(token);
                                }
                            }
                        }
                        catch ( FileNotFoundException )
                        {
                            StorageApplicationPermissions.FutureAccessList.Remove(token);
                        }
                        catch ( Exception )
                        {
                            StorageApplicationPermissions.FutureAccessList.Remove(token);
                        }
                    }
                    _localSettings.Values [FolderTokensKey] = string.Join("," , validTokens);
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saved folders: {ex}");
            }
        }

        [RelayCommand]
        public async Task AddFolderAsync ()
        {
            try
            {
                FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker , hwnd);

                StorageFolder folder = await openPicker.PickSingleFolderAsync();
                if ( folder != null && !Folders.Any(f => f.Path == folder.Path) )
                {
                    var token = StorageApplicationPermissions.FutureAccessList.Add(folder , folder.Path);
                    Folders.Add(folder);
                    SaveFolderTokens();
                    _folderScanCache.TryRemove(folder.Path , out _);
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine($"Error adding folder: {ex}");
                SearchStatus = "Không thể thêm thư mục.";
            }
        }

        [RelayCommand]
        public void RemoveFolder (StorageFolder folder)
        {
            if ( folder is null )
                return;

            try
            {
                var tokenToRemove = StorageApplicationPermissions.FutureAccessList.Entries
                    .Where(entry => entry.Metadata == folder.Path)
                    .Select(entry => entry.Token)
                    .FirstOrDefault();

                if ( !string.IsNullOrEmpty(tokenToRemove) )
                {
                    StorageApplicationPermissions.FutureAccessList.Remove(tokenToRemove);
                }

                Folders.Remove(folder);
                SelectedFolders.Remove(folder);
                _folderScanCache.TryRemove(folder.Path , out _);
                SaveFolderTokens();
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine($"Error removing folder: {ex}");
            }
        }

        private void SaveFolderTokens ()
        {
            try
            {
                var tokens = StorageApplicationPermissions.FutureAccessList.Entries
                    .Select(entry => entry.Token)
                    .ToList();

                _localSettings.Values [FolderTokensKey] = string.Join("," , tokens);
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine($"Error saving folder tokens: {ex}");
            }
        }

        [RelayCommand]
        public void ClearCache ()
        {
            _folderScanCache.Clear();
            SearchStatus = "Cache đã được xóa.";
        }

        public void Dispose ()
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
            _folderSemaphore?.Dispose();
            _searchSemaphore?.Dispose();
        }
    }
}