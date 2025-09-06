using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using winwallhaven.Core.Models;

namespace winwallhaven.Core.Wallpapers;

[SupportedOSPlatform("windows")]
public sealed class WindowsWallpaperService : IWallpaperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WindowsWallpaperService>? _logger;

    public WindowsWallpaperService(HttpClient httpClient, ILogger<WindowsWallpaperService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> DownloadAsync(Wallpaper wallpaper, string targetDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(targetDirectory);
        var fileName = wallpaper.Id + Path.GetExtension(wallpaper.Path);
        var localPath = Path.Combine(targetDirectory, fileName);
        if (File.Exists(localPath))
        {
            _logger?.LogDebug("Wallpaper already downloaded {Id} -> {Path}", wallpaper.Id, localPath);
            return localPath;
        }

        _logger?.LogInformation("Downloading wallpaper {Id} from {Remote}", wallpaper.Id, wallpaper.Path);
        using var resp = await _httpClient.GetAsync(wallpaper.Path, ct);
        resp.EnsureSuccessStatusCode();
        await using var fs = File.Create(localPath);
        await resp.Content.CopyToAsync(fs, ct);
        _logger?.LogInformation("Downloaded wallpaper {Id} size {Bytes} -> {Path}", wallpaper.Id, fs.Length, localPath);
        return localPath;
    }

    public async Task<string> DownloadToFileAsync(Wallpaper wallpaper, string filePath, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(dir)) throw new ArgumentException("Invalid file path", nameof(filePath));
        Directory.CreateDirectory(dir);

        _logger?.LogInformation("Downloading wallpaper {Id} to explicit path {Path} from {Remote}", wallpaper.Id,
            filePath, wallpaper.Path);
        using var resp = await _httpClient.GetAsync(wallpaper.Path, ct);
        resp.EnsureSuccessStatusCode();
        await using var fs = File.Create(filePath); // overwrite
        await resp.Content.CopyToAsync(fs, ct);
        _logger?.LogInformation("Downloaded wallpaper {Id} size {Bytes} -> {Path}", wallpaper.Id, fs.Length, filePath);
        return filePath;
    }

    public Task SetDesktopWallpaperAsync(string localFilePath)
    {
        if (!File.Exists(localFilePath)) throw new FileNotFoundException(localFilePath);
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new PlatformNotSupportedException();
        const uint SPI_SETDESKWALLPAPER = 0x0014;
        const uint SPIF_UPDATEINIFILE = 0x01;
        const uint SPIF_SENDWININICHANGE = 0x02;
        if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, localFilePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE))
        {
            var ex = new InvalidOperationException("Failed to set wallpaper");
            _logger?.LogError(ex, "Failed to set wallpaper file {Path}", localFilePath);
            throw ex;
        }

        _logger?.LogInformation("Wallpaper applied from {Path}", localFilePath);
        return Task.CompletedTask;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);
}