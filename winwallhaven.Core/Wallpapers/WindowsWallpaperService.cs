using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Storage;
using Windows.System.UserProfile;
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

    public async Task SetDesktopWallpaperAsync(string localFilePath)
    {
        if (!File.Exists(localFilePath)) throw new FileNotFoundException(localFilePath);
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new PlatformNotSupportedException();

        try
        {
            // Use the modern WinRT API for packaged applications (Windows 10+)
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
            {
                _logger?.LogDebug("Windows version check passed (>= 10.0.10240)");

                if (UserProfilePersonalizationSettings.IsSupported())
                {
                    _logger?.LogDebug("UserProfilePersonalizationSettings.IsSupported() returned true");

                    try
                    {
                        // Copy the file to app's local folder with a unique name to avoid duplicate filename issue
                        var localFolder = ApplicationData.Current.LocalFolder;
                        var uniqueFileName =
                            $"wallpaper_{Guid.NewGuid()}.{Path.GetExtension(localFilePath).TrimStart('.')}";
                        var destinationFile =
                            await localFolder.CreateFileAsync(uniqueFileName, CreationCollisionOption.ReplaceExisting);

                        _logger?.LogDebug("Created unique file in app local folder: {FileName}", uniqueFileName);

                        // Copy the original file to the app local folder
                        using var sourceStream = File.OpenRead(localFilePath);
                        using var destStream = await destinationFile.OpenStreamForWriteAsync();
                        await sourceStream.CopyToAsync(destStream);

                        _logger?.LogDebug(
                            "Copied file to app local folder. Original: {Original}, Destination: {Destination}",
                            localFilePath, destinationFile.Path);

                        var settings = UserProfilePersonalizationSettings.Current;
                        _logger?.LogDebug("Retrieved UserProfilePersonalizationSettings.Current");

                        _logger?.LogInformation("Calling TrySetWallpaperImageAsync for file: {Path}",
                            destinationFile.Path);
                        var result = await settings.TrySetWallpaperImageAsync(destinationFile);

                        _logger?.LogInformation("TrySetWallpaperImageAsync returned: {Result}", result);

                        if (result)
                        {
                            _logger?.LogInformation("Wallpaper applied from {Path} using WinRT API",
                                destinationFile.Path);
                        }
                        else
                        {
                            _logger?.LogError(
                                "WinRT API returned false - wallpaper setting failed. This might indicate missing manifest capabilities or policy restrictions.");
                            throw new InvalidOperationException(
                                "Failed to set wallpaper using WinRT API. Check app manifest for required capabilities.");
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger?.LogError(ex,
                            "UnauthorizedAccessException when using WinRT API for {Path}. This indicates insufficient permissions or missing manifest capabilities.",
                            localFilePath);
                        throw new InvalidOperationException(
                            "Insufficient permissions to set wallpaper. Check app manifest capabilities.", ex);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex,
                            "Exception when using WinRT API for {Path}. Exception type: {ExceptionType}", localFilePath,
                            ex.GetType().Name);
                        throw;
                    }
                }
                else
                {
                    _logger?.LogError(
                        "UserProfilePersonalizationSettings.IsSupported() returned false. The app context doesn't support personalization.");
                    throw new PlatformNotSupportedException(
                        "Wallpaper personalization is not supported in the current app context.");
                }
            }
            else
            {
                _logger?.LogError("Windows version check failed - not >= 10.0.10240");
                throw new PlatformNotSupportedException("Windows 10 or later is required for wallpaper setting.");
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException || ex is PlatformNotSupportedException))
        {
            _logger?.LogError(ex,
                "Unexpected exception when setting wallpaper for {Path}. Exception type: {ExceptionType}",
                localFilePath,
                ex.GetType().Name);
            throw new InvalidOperationException("Failed to set wallpaper due to unexpected error.", ex);
        }
    }
}