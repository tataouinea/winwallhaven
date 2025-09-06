using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml.Data;
using winwallhaven.Core.Models;
using winwallhaven.Core.Services;

namespace winwallhaven.Services;

/// <summary>
///     Observable collection that supports WinUI 3 incremental loading for GridView/ListView.
///     It pages via the provided fetch delegate and appends results as the user scrolls.
/// </summary>
public sealed class IncrementalWallpaperCollection : ObservableCollection<Wallpaper>, ISupportIncrementalLoading
{
    private readonly Func<int, CancellationToken, Task<WallpaperSearchResult>> _fetchPage;
    private readonly int? _maxItemCount;
    private int _currentPage;
    private int _isLoading; // 0 = false, 1 = true
    private int? _lastPage;

    public IncrementalWallpaperCollection(Func<int, CancellationToken, Task<WallpaperSearchResult>> fetchPage,
        int? maxItemCount = 500)
    {
        _fetchPage = fetchPage;
        _currentPage = 0; // Will request page 1 on first load
        _maxItemCount = maxItemCount;
    }

    public Action<int, int?>? PageInfoUpdated { get; set; }

    public bool HasMoreItems => _lastPage is null || _currentPage < _lastPage;

    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        return LoadMoreItemsInternalAsync(count).AsAsyncOperation();
    }

    private async Task<LoadMoreItemsResult> LoadMoreItemsInternalAsync(uint _)
    {
        if (Interlocked.Exchange(ref _isLoading, 1) == 1)
            // Already loading; return 0 to avoid reentrancy
            return new LoadMoreItemsResult { Count = 0 };

        try
        {
            // Determine next page (first call goes to page 1)
            var nextPage = _currentPage == 0 ? 1 : _currentPage + 1;
            var result = await _fetchPage(nextPage, CancellationToken.None);
            _currentPage = result.CurrentPage;
            _lastPage = result.LastPage;

            PageInfoUpdated?.Invoke(_currentPage, _lastPage);

            foreach (var item in result.Items)
                // UI thread dispatch is handled by ObservableCollection in WinUI context
                Add(item);

            // Trim head if exceeding max allowed items to keep memory bounded
            if (_maxItemCount is int max && max > 0 && Count > max)
            {
                var removeCount = Count - max;
                while (removeCount-- > 0 && Count > 0) RemoveAt(0);
            }

            return new LoadMoreItemsResult { Count = (uint)result.Items.Count };
        }
        catch
        {
            // In case of error, stop further auto-loads this session
            _lastPage = _currentPage; // No more items
            return new LoadMoreItemsResult { Count = 0 };
        }
        finally
        {
            Interlocked.Exchange(ref _isLoading, 0);
        }
    }
}