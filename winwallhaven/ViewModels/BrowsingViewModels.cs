using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
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
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private bool _showApiLink;

    protected BrowsingViewModelBase(IWallhavenApiClient api, IWallpaperService wallpaperService, ILogger? logger = null)
    {
        Api = api;
        Logger = logger;
        RefreshCommand = new AsyncRelayCommand(LoadFirstPageAsync, () => !IsLoading);
        FirstPageCommand = new AsyncRelayCommand(LoadFirstPageAsync, () => !IsLoading && CanLoadPrevPage);
        NextPageCommand = new AsyncRelayCommand(LoadNextPageAsync, () => !IsLoading && CanLoadNextPage);
        PrevPageCommand = new AsyncRelayCommand(LoadPrevPageAsync, () => !IsLoading && CanLoadPrevPage);
        ApplyFiltersCommand = new AsyncRelayCommand(LoadFirstPageAsync, () => !IsLoading);
        RetryCommand = new AsyncRelayCommand(LoadFirstPageAsync, () => !IsLoading);
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

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool ShowApiLink
    {
        get => _showApiLink;
        private set => SetProperty(ref _showApiLink, value);
    }

    public bool CanLoadNextPage => LastPage == null || CurrentPage < LastPage;
    public bool CanLoadPrevPage => CurrentPage > 1;

    public ICommand RefreshCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand RetryCommand { get; }
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
        if (IsLoading) return;
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
        var target = CurrentPage + 1;
        return LoadTargetPageAsync(target);
    }

    private Task LoadPrevPageAsync()
    {
        if (!CanLoadPrevPage) return Task.CompletedTask;
        var target = CurrentPage - 1;
        return LoadTargetPageAsync(target);
    }

    private async Task<bool> LoadPageAsync()
    {
        IsLoading = true;
        HasError = false;
        ShowApiLink = false;
        ErrorMessage = string.Empty;
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
            return true;
        }
        catch (InvalidOperationException)
        {
            // No items or logical page beyond real end; treat as navigation stop and revert.
            HasError = false;
            ErrorMessage = string.Empty;
            return false;
        }
        catch (TimeoutException tex)
        {
            Logger?.LogError(tex, "Timeout while loading page");
            HasError = true;
            ShowApiLink = true;
            ErrorMessage = BuildTimeoutMessage(30);
            return false;
        }
        catch (HttpRequestException hre)
        {
            Logger?.LogError(hre, "HTTP failure while loading page");
            HasError = true;
            ShowApiLink = !hre.StatusCode.HasValue;
            ErrorMessage = BuildHttpFailureMessage(hre);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTargetPageAsync(int target)
    {
        var previous = CurrentPage;
        CurrentPage = target;
        var success = await LoadPageAsync();

        if (!success)
        {
            CurrentPage = previous;
            await LoadPageAsync();
        }
    }

    private void RaiseCanExec()
    {
        (RefreshCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (FirstPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrevPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ApplyFiltersCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RetryCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        _actions.RaiseCanExec();
    }

    private static string DescribeStatus(HttpStatusCode status)
    {
        return status switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.Forbidden => "Forbidden",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.RequestTimeout => "Request Timeout",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            HttpStatusCode.BadGateway => "Bad Gateway",
            HttpStatusCode.ServiceUnavailable => "Service Unavailable",
            HttpStatusCode.GatewayTimeout => "Gateway Timeout",
            _ => status.ToString()
        };
    }

    private static string BuildHttpFailureMessage(HttpRequestException hre)
    {
        var statusCodeProp = hre.StatusCode;
        if (statusCodeProp.HasValue)
        {
            var status = statusCodeProp.Value;
            var code = (int)status;
            var reason = DescribeStatus(status);
            return $"HTTP status {code} ({reason}).";
        }
        return BuildGenericFailureMessage();
    }

    private static string BuildGenericFailureMessage()
    {
        return "The request failed due to a connectivity error. Please ensure your internet connection works and that wallhaven.cc is reachable. You can also click the link below to verify wallhaven.cc's API is accessible from your device.";
    }

    private static string BuildTimeoutMessage(int seconds)
    {
        return $"The request timed out after {seconds} seconds. This may indicate a slow or unstable network, or that wallhaven.cc took too long to respond. You can also click the link below to verify wallhaven.cc's API is accessible from your device.";
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