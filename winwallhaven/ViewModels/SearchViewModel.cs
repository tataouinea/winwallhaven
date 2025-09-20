using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
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

    public SearchViewModel(IWallhavenApiClient apiClient, IWallpaperService wallpaperService,
        ILogger<SearchViewModel>? logger = null)
    {
        _apiClient = apiClient;
        _logger = logger;
        _wallpaperService = wallpaperService;
        SearchCommand = new AsyncRelayCommand(SearchFirstPageAsync, () => !IsLoading);
        ApplyFiltersCommand = new AsyncRelayCommand(SearchFirstPageAsync, () => !IsLoading);
        NextPageCommand = new AsyncRelayCommand(LoadNextPageAsync, () => !IsLoading && CanLoadNextPage);
        PrevPageCommand = new AsyncRelayCommand(LoadPrevPageAsync, () => !IsLoading && CanLoadPrevPage);
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
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand OpenInBrowserCommand => _actions.OpenInBrowserCommand;
    public ICommand OpenUserProfileCommand => _actions.OpenUserProfileCommand;
    public ICommand SetAsWallpaperCommand => _actions.SetAsWallpaperCommand;
    public ICommand SetAsLockScreenCommand => _actions.SetAsLockScreenCommand;
    public ICommand SetAsBothCommand => _actions.SetAsBothCommand;
    public ICommand DownloadCommand => _actions.DownloadCommand;
    public ICommand RemoveFromHistoryCommand => _actions.RemoveFromHistoryCommand;

    private Window AppWindow => App.MainAppWindow!;

    private async Task LoadPageAsync()
    {
        IsLoading = true;
        try
        {
            Results.Clear();
            var sw = Stopwatch.StartNew();
            var sorting = string.IsNullOrWhiteSpace(Query) ? "date_added" : "relevance";
            var virtualPage = await _paginator.GetAsync(
                apiPage => new WallpaperSearchQuery(Query,
                    Filters.GetCategoriesParam(),
                    Filters.GetPurityParam(),
                    sorting, "desc", apiPage), CurrentPage);
            CurrentPage = virtualPage.LogicalPage;
            LastPage = virtualPage.LastLogicalPage; // likely null until end detected
            foreach (var w in virtualPage.Items) Results.Add(w);
            sw.Stop();
            _logger?.LogInformation("Loaded logical page {Page} with {Count} results in {Ms}ms", CurrentPage,
                Results.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load page");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task SearchFirstPageAsync()
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

    private void RaiseCanExec()
    {
        (SearchCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ApplyFiltersCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrevPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        _actions.RaiseCanExec();
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