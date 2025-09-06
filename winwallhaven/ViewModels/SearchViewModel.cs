using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using winwallhaven.Core.Models;
using winwallhaven.Core.Services;
using winwallhaven.Core.Wallpapers;
using winwallhaven.Services;
#if WINDOWS
using Windows.Storage.Pickers;
using WinRT.Interop;
#endif

namespace winwallhaven.ViewModels;

public sealed class SearchViewModel : ViewModelBase
{
    private readonly IWallhavenApiClient _apiClient;
    private readonly ILogger<SearchViewModel>? _logger;
    private readonly IWallpaperService _wallpaperService;
    private int _currentPage;
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
        Results = new IncrementalWallpaperCollection((page, ct) =>
        {
            var sorting = string.IsNullOrWhiteSpace(Query) ? "date_added" : "relevance";
            return _apiClient.SearchAsync(new WallpaperSearchQuery(Query, "111", "100", sorting, "desc", page), ct);
        });
        Results.PageInfoUpdated = (cp, lp) =>
        {
            CurrentPage = cp;
            LastPage = lp;
        };
        SearchCommand = new AsyncRelayCommand(ResetAndSearchAsync, () => !IsLoading);
        OpenInBrowserCommand = new RelayCommand<Wallpaper?>(OpenInBrowser, w => w != null);
        SetAsWallpaperCommand = new AsyncRelayCommand<Wallpaper?>(SetAsWallpaperAsync, w => w != null && !IsLoading);
#if WINDOWS
#pragma warning disable CA1416
        DownloadCommand = new AsyncRelayCommand<Wallpaper?>(DownloadAsync, w => w != null && !IsLoading);
#pragma warning restore CA1416
#else
    DownloadCommand = new AsyncRelayCommand<Wallpaper?>(_ => Task.CompletedTask, _ => false);
#endif
    }

    public IncrementalWallpaperCollection Results { get; }

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

    public ICommand SearchCommand { get; }
    public ICommand OpenInBrowserCommand { get; }
    public ICommand SetAsWallpaperCommand { get; }
    public ICommand DownloadCommand { get; }

    private Window AppWindow => App.MainAppWindow!;

    private async Task LoadPageAsync()
    {
        // Not used anymore - kept only to satisfy any potential callers
        await Task.CompletedTask;
    }

    private async Task ResetAndSearchAsync()
    {
        IsLoading = true;
        try
        {
            Results.Clear();
            _currentPage = 0;
            _lastPage = null;
            await Results.LoadMoreItemsAsync(1).AsTask();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenInBrowser(Wallpaper? w)
    {
        if (w == null) return;
        try
        {
            var psi = new ProcessStartInfo { FileName = w.Url, UseShellExecute = true };
            Process.Start(psi);
        }
        catch
        {
        }
    }

    private async Task SetAsWallpaperAsync(Wallpaper? w)
    {
        if (w == null) return;
        try
        {
            var cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "winwallhaven", "cache");
            Directory.CreateDirectory(cache);
            var local = await _wallpaperService.DownloadAsync(w, cache);
            await _wallpaperService.SetDesktopWallpaperAsync(local);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set wallpaper");
        }
    }

#if WINDOWS
    [SupportedOSPlatform("windows10.0.10240")]
    private async Task DownloadAsync(Wallpaper? w)
    {
        if (w == null) return;
        try
        {
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add("Image", new List<string> { Path.GetExtension(w.Path) });
            picker.SuggestedFileName = w.Id + Path.GetExtension(w.Path);
            var hwnd = WindowNative.GetWindowHandle(AppWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSaveFileAsync();
            if (file == null) return;
            await _wallpaperService.DownloadToFileAsync(w, file.Path);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download wallpaper");
        }
    }
#endif

    private void RaiseCanExec()
    {
        (SearchCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OpenInBrowserCommand as RelayCommand<Wallpaper?>)?.RaiseCanExecuteChanged();
        (SetAsWallpaperCommand as AsyncRelayCommand<Wallpaper?>)?.RaiseCanExecuteChanged();
        (DownloadCommand as AsyncRelayCommand<Wallpaper?>)?.RaiseCanExecuteChanged();
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