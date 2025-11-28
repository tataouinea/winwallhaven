using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
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
#if WINDOWS
#endif

namespace winwallhaven.ViewModels;

public sealed class SearchViewModel : ViewModelBase
{
    private readonly WallpaperActions _actions;
    private readonly IWallhavenApiClient _apiClient;
    private readonly ILogger<SearchViewModel>? _logger;
    private readonly VirtualWallpaperPaginator _paginator;
    private readonly IWallpaperService _wallpaperService;
    private int _currentPage = 1;
    private bool _isLoading;
    private int? _lastPage;
    private string _query = string.Empty;
    private Wallpaper? _selected;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private bool _showApiLink;

    public SearchViewModel(IWallhavenApiClient apiClient, IWallpaperService wallpaperService,
        ILogger<SearchViewModel>? logger = null)
    {
        _apiClient = apiClient;
        _logger = logger;
        _wallpaperService = wallpaperService;
        SearchCommand = new AsyncRelayCommand(SearchFirstPageAsync, () => !IsLoading);
        ApplyFiltersCommand = new AsyncRelayCommand(SearchFirstPageAsync, () => !IsLoading);
        FirstPageCommand = new AsyncRelayCommand(LoadFirstPageAsync, () => !IsLoading && CanLoadPrevPage);
        NextPageCommand = new AsyncRelayCommand(LoadNextPageAsync, () => !IsLoading && CanLoadNextPage);
        PrevPageCommand = new AsyncRelayCommand(LoadPrevPageAsync, () => !IsLoading && CanLoadPrevPage);
        RetryCommand = new AsyncRelayCommand(SearchFirstPageAsync, () => !IsLoading);
        _actions = new WallpaperActions(wallpaperService, App.Services.GetRequiredService<IHistoryService>(), logger,
            () => IsLoading);
        _paginator = new VirtualWallpaperPaginator(apiClient);
    }

    public ObservableCollection<Wallpaper> Results { get; } = new();

    public FilterOptions Filters { get; } = new();

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

    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value ?? string.Empty);
    }

    public Wallpaper? Selected
    {
        get => _selected;
        set
        {
            SetProperty(ref _selected, value);
            RaiseCanExec();
        }
    }

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

    public bool CanLoadNextPage => LastPage == null || CurrentPage < LastPage;
    public bool CanLoadPrevPage => CurrentPage > 1;

    public ICommand SearchCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand RetryCommand { get; }
    public ICommand OpenInBrowserCommand => _actions.OpenInBrowserCommand;
    public ICommand OpenUserProfileCommand => _actions.OpenUserProfileCommand;
    public ICommand SetAsWallpaperCommand => _actions.SetAsWallpaperCommand;
    public ICommand SetAsLockScreenCommand => _actions.SetAsLockScreenCommand;
    public ICommand SetAsBothCommand => _actions.SetAsBothCommand;
    public ICommand DownloadCommand => _actions.DownloadCommand;
    public ICommand RemoveFromHistoryCommand => _actions.RemoveFromHistoryCommand;

    private Window AppWindow => App.MainAppWindow!;

    private async Task<bool> LoadPageAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        ShowApiLink = false;
        try
        {
            Results.Clear();
            var sw = Stopwatch.StartNew();
            var sorting = string.IsNullOrWhiteSpace(Query) ? "date_added" : "relevance";
            var minW = Filters.MinWidth > 0 ? Filters.MinWidth : null;
            var minH = Filters.MinHeight > 0 ? Filters.MinHeight : null;
            var virtualPage = await _paginator.GetAsync(
                apiPage => new WallpaperSearchQuery(Query,
                    Filters.GetCategoriesParam(),
                    Filters.GetPurityParam(),
                    sorting, "desc", apiPage,
                    minW, minH), CurrentPage);
            CurrentPage = virtualPage.LogicalPage;
            LastPage = virtualPage.LastLogicalPage; // likely null until end detected
            foreach (var w in virtualPage.Items) Results.Add(w);
            sw.Stop();
            _logger?.LogInformation("Loaded logical page {Page} with {Count} results in {Ms}ms", CurrentPage,
                Results.Count, sw.ElapsedMilliseconds);
            return true;
        }
        catch (InvalidOperationException)
        {
            HasError = false;
            ErrorMessage = string.Empty;
            return false;
        }
        catch (TimeoutException tex)
        {
            _logger?.LogError(tex, "Timeout while loading page");
            HasError = true;
            ShowApiLink = true;
            ErrorMessage = BuildTimeoutMessage(30);
            return false;
        }
        catch (HttpRequestException hre)
        {
            _logger?.LogError(hre, "HTTP failure while loading page");
            HasError = true;
            ShowApiLink = !hre.StatusCode.HasValue; // show link only when we don't have an HTTP status
            ErrorMessage = BuildHttpFailureMessage(hre);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchFirstPageAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync();
    }

    private Task LoadFirstPageAsync()
    {
        if (!CanLoadPrevPage && CurrentPage == 1) return Task.CompletedTask;
        var target = 1;
        return LoadTargetPageAsync(target);
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

    private async Task LoadTargetPageAsync(int targetPage)
    {
        var previous = CurrentPage;
        CurrentPage = targetPage;
        var ok = await LoadPageAsync();
        if (!ok)
        {
            CurrentPage = previous; // revert if failed
            await LoadPageAsync(); // ensure UI shows previous page again
        }
    }

    private void RaiseCanExec()
    {
        (SearchCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ApplyFiltersCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (FirstPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrevPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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
        // No status code (e.g. name resolution failure, no internet) -> show generic guidance and link
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

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<bool>? _can;
    private readonly Func<Task> _exec;
    private bool _running;

    public AsyncRelayCommand(Func<Task> exec, Func<bool>? can = null)
    {
        _exec = exec;
        _can = can;
    }

    public bool CanExecute(object? parameter)
    {
        return !_running && (_can?.Invoke() ?? true);
    }

    public event EventHandler? CanExecuteChanged;

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _running = true;
        RaiseCanExecuteChanged();
        try
        {
            await _exec();
        }
        finally
        {
            _running = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, bool>? _can;
    private readonly Func<T?, Task> _exec;
    private bool _running;

    public AsyncRelayCommand(Func<T?, Task> exec, Func<T?, bool>? can = null)
    {
        _exec = exec;
        _can = can;
    }

    public bool CanExecute(object? parameter)
    {
        return !_running && (_can?.Invoke((T?)parameter) ?? true);
    }

    public event EventHandler? CanExecuteChanged;

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _running = true;
        RaiseCanExecuteChanged();
        try
        {
            await _exec((T?)parameter);
        }
        finally
        {
            _running = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Func<T?, bool>? _can;
    private readonly Action<T?> _exec;

    public RelayCommand(Action<T?> exec, Func<T?, bool>? can = null)
    {
        _exec = exec;
        _can = can;
    }

    public bool CanExecute(object? parameter)
    {
        return _can?.Invoke((T?)parameter) ?? true;
    }

    public event EventHandler? CanExecuteChanged;

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _exec((T?)parameter);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}