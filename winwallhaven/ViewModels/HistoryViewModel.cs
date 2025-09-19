using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using winwallhaven.Core.Models;
using winwallhaven.Core.Wallpapers;
using winwallhaven.Services;

namespace winwallhaven.ViewModels;

public sealed class HistoryViewModel : ViewModelBase
{
    private const int PageSize = 96;
    private readonly WallpaperActions _actions;
    private readonly IHistoryService _history;
    private readonly ILogger<HistoryViewModel>? _logger;
    private int _currentPage = 1;

    private bool _isLoading;
    private int? _lastPage;

    public HistoryViewModel(IHistoryService history, ILogger<HistoryViewModel>? logger = null)
    {
        _history = history;
        _logger = logger;
        _actions = new WallpaperActions(App.Services.GetRequiredService<IWallpaperService>(), history, logger,
            () => IsLoading);
        RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsLoading);
        NextPageCommand = new AsyncRelayCommand(LoadNextPageAsync, () => !IsLoading && CanLoadNextPage);
        PrevPageCommand = new AsyncRelayCommand(LoadPrevPageAsync, () => !IsLoading && CanLoadPrevPage);
        RemoveFromHistoryCommand = new AsyncRelayCommand<Wallpaper?>(RemoveAsync, w => w != null && !IsLoading);
        ClearHistoryCommand = new AsyncRelayCommand(ClearAsync, () => !IsLoading);
    }

    public ObservableCollection<Wallpaper> Results { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            SetProperty(ref _isLoading, value);
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

    public ICommand RefreshCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand RemoveFromHistoryCommand { get; }

    public ICommand ClearHistoryCommand { get; }

    // Expose shared actions used by context menu
    public ICommand OpenInBrowserCommand => _actions.OpenInBrowserCommand;
    public ICommand SetAsWallpaperCommand => _actions.SetAsWallpaperCommand;
    public ICommand SetAsLockScreenCommand => _actions.SetAsLockScreenCommand;
    public ICommand SetAsBothCommand => _actions.SetAsBothCommand;
    public ICommand DownloadCommand => _actions.DownloadCommand;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Results.Clear();
            var all = await _history.GetAllAsync();
            var total = all.Count;
            LastPage = total == 0 ? 1 : (int)Math.Ceiling(total / (double)PageSize);
            CurrentPage = Math.Clamp(CurrentPage, 1, LastPage ?? 1);
            var pageItems = all
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(h => h.Wallpaper);
            foreach (var w in pageItems) Results.Add(w);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load history page");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadNextPageAsync()
    {
        if (!CanLoadNextPage) return;
        CurrentPage++;
        await LoadAsync();
    }

    private async Task LoadPrevPageAsync()
    {
        if (!CanLoadPrevPage) return;
        CurrentPage--;
        await LoadAsync();
    }

    private void RaiseCanExec()
    {
        (RefreshCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrevPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RemoveFromHistoryCommand as AsyncRelayCommand<Wallpaper?>)?.RaiseCanExecuteChanged();
        (ClearHistoryCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        _actions.RaiseCanExec();
    }

    private async Task RemoveAsync(Wallpaper? w)
    {
        if (w == null) return;
        try
        {
            await _history.RemoveAsync(w.Id);
            // Refresh current page maintaining index
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove from history: {Id}", w.Id);
        }
    }

    private async Task ClearAsync()
    {
        try
        {
            await _history.ClearAsync();
            CurrentPage = 1;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear history");
        }
    }
}