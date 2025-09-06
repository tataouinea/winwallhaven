using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using winwallhaven.Core.Services;
using winwallhaven.Services;

namespace winwallhaven.ViewModels;

public abstract class BrowsingViewModelBase : ViewModelBase
{
    protected readonly IWallhavenApiClient Api;
    protected readonly ILogger? Logger;
    private int _currentPage;

    private bool _isRefreshing;
    private int? _lastPage;

    protected BrowsingViewModelBase(IWallhavenApiClient api, ILogger? logger = null)
    {
        Api = api;
        Logger = logger;
        RefreshCommand = new AsyncRelayCommand(ResetAndReloadAsync, () => !IsRefreshing);
        Results = new IncrementalWallpaperCollection((page, ct) => Api.SearchAsync(BuildQuery(page), ct));
        Results.PageInfoUpdated = (cp, lp) =>
        {
            CurrentPage = cp;
            LastPage = lp;
        };
    }

    public IncrementalWallpaperCollection Results { get; }

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            SetProperty(ref _currentPage, value);
            RaiseCanExec();
        }
    }

    public int? LastPage
    {
        get => _lastPage;
        private set
        {
            SetProperty(ref _lastPage, value);
            RaiseCanExec();
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            SetProperty(ref _isRefreshing, value);
            RaiseCanExec();
        }
    }

    public ICommand RefreshCommand { get; }

    protected abstract WallpaperSearchQuery BuildQuery(int page);

    public async Task LoadAsync()
    {
        if (Results.Count == 0) await ResetAndReloadAsync();
    }

    private async Task ResetAndReloadAsync()
    {
        IsRefreshing = true;
        try
        {
            Results.Clear();
            _currentPage = 0;
            _lastPage = null;
            // Trigger initial load by asking for one batch
            await Results.LoadMoreItemsAsync(1).AsTask();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to refresh");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void RaiseCanExec()
    {
        (RefreshCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}

public sealed class LatestViewModel : BrowsingViewModelBase
{
    public LatestViewModel(IWallhavenApiClient api, ILogger<LatestViewModel>? logger = null) : base(api, logger)
    {
    }

    protected override WallpaperSearchQuery BuildQuery(int page)
    {
        return new WallpaperSearchQuery("", "111", "100", "date_added", "desc", page);
    }
}

public sealed class ToplistViewModel : BrowsingViewModelBase
{
    public ToplistViewModel(IWallhavenApiClient api, ILogger<ToplistViewModel>? logger = null) : base(api, logger)
    {
    }

    protected override WallpaperSearchQuery BuildQuery(int page)
    {
        return new WallpaperSearchQuery("", "111", "100", "toplist", "desc", page);
    }
}

public sealed class RandomViewModel : BrowsingViewModelBase
{
    public RandomViewModel(IWallhavenApiClient api, ILogger<RandomViewModel>? logger = null) : base(api, logger)
    {
    }

    protected override WallpaperSearchQuery BuildQuery(int page)
    {
        return new WallpaperSearchQuery("", "111", "100", "random", "desc", page);
    }
}