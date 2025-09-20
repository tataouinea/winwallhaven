using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using winwallhaven.Core.Models;
using winwallhaven.Core.Services;
using winwallhaven.Core.Wallpapers;
using winwallhaven.Services;

namespace winwallhaven.ViewModels;

public abstract class BrowsingViewModelBase : ViewModelBase
{
    private readonly WallpaperActions _actions;
    private readonly VirtualWallpaperPaginator _paginator;
    private readonly IWallpaperService _wallpaperService;
    protected readonly IWallhavenApiClient Api;
    protected readonly ILogger? Logger;

    private int _currentPage = 1;
    private bool _isLoading;
    private int? _lastPage;

    protected BrowsingViewModelBase(IWallhavenApiClient api, IWallpaperService wallpaperService, ILogger? logger = null)
    {
        Api = api;
        Logger = logger;
        RefreshCommand = new AsyncRelayCommand(LoadFirstPageAsync, () => !IsLoading);
        NextPageCommand = new AsyncRelayCommand(LoadNextPageAsync, () => !IsLoading && CanLoadNextPage);
        PrevPageCommand = new AsyncRelayCommand(LoadPrevPageAsync, () => !IsLoading && CanLoadPrevPage);
        ApplyFiltersCommand = new AsyncRelayCommand(LoadFirstPageAsync, () => !IsLoading);
        _wallpaperService = wallpaperService;
        _actions = new WallpaperActions(wallpaperService, App.Services.GetRequiredService<IHistoryService>(), logger,
            () => IsLoading);
        _paginator = new VirtualWallpaperPaginator(api);
    }

    public ObservableCollection<Wallpaper> Results { get; } = new();

    public FilterOptions Filters { get; } = new();

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

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            SetProperty(ref _isLoading, value);
            RaiseCanExec();
        }
    }

    public bool CanLoadNextPage => LastPage == null || CurrentPage < LastPage;
    public bool CanLoadPrevPage => CurrentPage > 1;

    public ICommand RefreshCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand OpenInBrowserCommand => _actions.OpenInBrowserCommand;
    public ICommand OpenUserProfileCommand => _actions.OpenUserProfileCommand;
    public ICommand SetAsWallpaperCommand => _actions.SetAsWallpaperCommand;
    public ICommand SetAsLockScreenCommand => _actions.SetAsLockScreenCommand;
    public ICommand SetAsBothCommand => _actions.SetAsBothCommand;
    public ICommand DownloadCommand => _actions.DownloadCommand;
    public ICommand RemoveFromHistoryCommand => _actions.RemoveFromHistoryCommand;

    protected abstract WallpaperSearchQuery BuildQuery(int page);

    public async Task LoadAsync()
    {
        if (Results.Count == 0)
            await LoadFirstPageAsync();
    }

    private Task LoadFirstPageAsync()
    {
        CurrentPage = 1;
        return LoadPageAsync();
    }

    private Task LoadNextPageAsync()
    {
        if (!CanLoadNextPage) return Task.CompletedTask;
        CurrentPage++;
        return LoadPageAsync();
    }

    private Task LoadPrevPageAsync()
    {
        if (!CanLoadPrevPage) return Task.CompletedTask;
        CurrentPage--;
        return LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        IsLoading = true;
        try
        {
            Results.Clear();
            var sw = Stopwatch.StartNew();
            var virtualPage = await _paginator.GetAsync(p => BuildQuery(p), CurrentPage);
            CurrentPage = virtualPage.LogicalPage;
            LastPage = virtualPage.LastLogicalPage;
            foreach (var w in virtualPage.Items) Results.Add(w);
            sw.Stop();
            (Logger as ILogger<BrowsingViewModelBase>)?.LogInformation("Loaded {Count} results in {Ms}ms",
                Results.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to load page");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RaiseCanExec()
    {
        (RefreshCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrevPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ApplyFiltersCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        _actions.RaiseCanExec();
    }
}

public sealed class LatestViewModel : BrowsingViewModelBase
{
    public LatestViewModel(IWallhavenApiClient api, IWallpaperService wallpaperService,
        ILogger<LatestViewModel>? logger = null) : base(api, wallpaperService, logger)
    {
    }

    protected override WallpaperSearchQuery BuildQuery(int page)
    {
        var minW = Filters.MinWidth > 0 ? Filters.MinWidth : null;
        var minH = Filters.MinHeight > 0 ? Filters.MinHeight : null;
        return new WallpaperSearchQuery("",
            Filters.GetCategoriesParam(),
            Filters.GetPurityParam(),
            "date_added", "desc", page,
            minW, minH);
    }
}

public sealed class ToplistViewModel : BrowsingViewModelBase
{
    public ToplistViewModel(IWallhavenApiClient api, IWallpaperService wallpaperService,
        ILogger<ToplistViewModel>? logger = null) : base(api, wallpaperService, logger)
    {
    }

    protected override WallpaperSearchQuery BuildQuery(int page)
    {
        var minW = Filters.MinWidth > 0 ? Filters.MinWidth : null;
        var minH = Filters.MinHeight > 0 ? Filters.MinHeight : null;
        return new WallpaperSearchQuery("",
            Filters.GetCategoriesParam(),
            Filters.GetPurityParam(),
            "toplist", "desc", page,
            minW, minH);
    }
}

public sealed class RandomViewModel : BrowsingViewModelBase
{
    public RandomViewModel(IWallhavenApiClient api, IWallpaperService wallpaperService,
        ILogger<RandomViewModel>? logger = null) : base(api, wallpaperService, logger)
    {
    }

    protected override WallpaperSearchQuery BuildQuery(int page)
    {
        var minW = Filters.MinWidth > 0 ? Filters.MinWidth : null;
        var minH = Filters.MinHeight > 0 ? Filters.MinHeight : null;
        return new WallpaperSearchQuery("",
            Filters.GetCategoriesParam(),
            Filters.GetPurityParam(),
            "random", "desc", page,
            minW, minH);
    }
}