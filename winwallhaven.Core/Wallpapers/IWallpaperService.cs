using winwallhaven.Core.Models;

namespace winwallhaven.Core.Wallpapers;

public interface IWallpaperService
{
    Task<string> DownloadAsync(Wallpaper wallpaper, string targetDirectory, CancellationToken ct = default);
    Task<string> DownloadToFileAsync(Wallpaper wallpaper, string filePath, CancellationToken ct = default);
    Task SetDesktopWallpaperAsync(string localFilePath);
}