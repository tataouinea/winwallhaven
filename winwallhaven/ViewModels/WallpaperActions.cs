using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using winwallhaven.Core.Models;
using winwallhaven.Core.Wallpapers;
#if WINDOWS
using Windows.Storage.Pickers;
using WinRT.Interop;
#endif

namespace winwallhaven.ViewModels;

/// <summary>
///     Provides reusable wallpaper interaction commands (open in browser, download, set as wallpaper).
///     Consumes existing AsyncRelayCommand / RelayCommand types defined in this namespace.
/// </summary>
public sealed class WallpaperActions
{
    private readonly Func<bool> _isBusy;
    private readonly ILogger? _logger;
    private readonly IWallpaperService _wallpaperService;

    public WallpaperActions(IWallpaperService wallpaperService, ILogger? logger, Func<bool> isBusy)
    {
        _wallpaperService = wallpaperService;
        _logger = logger;
        _isBusy = isBusy;

        OpenInBrowserCommand = new RelayCommand<Wallpaper?>(OpenInBrowser, w => w != null);
        SetAsWallpaperCommand = new AsyncRelayCommand<Wallpaper?>(SetAsWallpaperAsync, w => w != null && !_isBusy());
        SetAsLockScreenCommand = new AsyncRelayCommand<Wallpaper?>(SetAsLockScreenAsync, w => w != null && !_isBusy());
#if WINDOWS
#pragma warning disable CA1416
        DownloadCommand = new AsyncRelayCommand<Wallpaper?>(DownloadAsync, w => w != null && !_isBusy());
#pragma warning restore CA1416
#else
        DownloadCommand = new AsyncRelayCommand<Wallpaper?>(_ => Task.CompletedTask, _ => false);
#endif
    }

    public ICommand OpenInBrowserCommand { get; }
    public ICommand SetAsWallpaperCommand { get; }
    public ICommand SetAsLockScreenCommand { get; }
    public ICommand DownloadCommand { get; }

    public void RaiseCanExec()
    {
        (OpenInBrowserCommand as RelayCommand<Wallpaper?>)?.RaiseCanExecuteChanged();
        (SetAsWallpaperCommand as AsyncRelayCommand<Wallpaper?>)?.RaiseCanExecuteChanged();
        (SetAsLockScreenCommand as AsyncRelayCommand<Wallpaper?>)?.RaiseCanExecuteChanged();
        (DownloadCommand as AsyncRelayCommand<Wallpaper?>)?.RaiseCanExecuteChanged();
    }

    private void OpenInBrowser(Wallpaper? w)
    {
        if (w == null) return;
        try
        {
            var psi = new ProcessStartInfo { FileName = w.Url, UseShellExecute = true };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open browser for {Id}", w.Id);
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
            _logger?.LogError(ex, "Failed to set wallpaper for {Id}", w.Id);
        }
    }

    private async Task SetAsLockScreenAsync(Wallpaper? w)
    {
        if (w == null) return;
        try
        {
            var cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "winwallhaven", "cache");
            Directory.CreateDirectory(cache);
            var local = await _wallpaperService.DownloadAsync(w, cache);
            await _wallpaperService.SetLockScreenImageAsync(local);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set lock screen image for {Id}", w.Id);
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
            var ext = Path.GetExtension(w.Path);
            if (!string.IsNullOrWhiteSpace(ext))
                picker.FileTypeChoices.Add("Image", new List<string> { ext });
            picker.SuggestedFileName = w.Id + ext;
            var hwnd = WindowNative.GetWindowHandle(App.MainAppWindow!);
            InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSaveFileAsync();
            if (file == null) return;
            await _wallpaperService.DownloadToFileAsync(w, file.Path);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download wallpaper {Id}", w.Id);
        }
    }
#endif
}