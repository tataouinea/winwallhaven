using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using winwallhaven.Core.Models;
using winwallhaven.Core.Services;

namespace winwallhaven.ViewModels;

public abstract class BrowsingViewModelBase : ViewModelBase
{
    protected readonly IWallhavenApiClient Api;
    protected readonly ILogger? Logger;

    private int _currentPage = 1;
    private bool _isLoading;
    private int? _lastPage;

    protected BrowsingViewModelBase(IWallhavenApiClient api, ILogger? logger = null)
    {
        Api = api;
        Logger = logger;
        RefreshCommand = new AsyncRelayCommand(LoadFirstPageAsync, () => !IsLoading);
        NextPageCommand = new AsyncRelayCommand(LoadNextPageAsync, () => !IsLoading && CanLoadNextPage);
        PrevPageCommand = new AsyncRelayCommand(LoadPrevPageAsync, () => !IsLoading && CanLoadPrevPage);
    }

    public ObservableCollection<Wallpaper> Results { get; } = new();

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
            var query = BuildQuery(CurrentPage);
            var result = await Api.SearchAsync(query);
            CurrentPage = result.CurrentPage;
            LastPage = result.LastPage;
            foreach (var w in result.Items) Results.Add(w);
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