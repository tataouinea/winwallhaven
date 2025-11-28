using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.IO;
using Windows.Storage;
using winwallhaven.Core.Services;
using winwallhaven.Core.Wallpapers;
using winwallhaven.Services;
using winwallhaven.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winwallhaven;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    public static IServiceProvider Services { get; private set; } = null!;
    public static Window? MainAppWindow { get; private set; }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // Configure Serilog first
            ConfigureSerilog();

            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            _window = new MainWindow();
            MainAppWindow = _window;
            // Apply the persisted theme preference (default System)
            var themeService = Services.GetService(typeof(IThemeService)) as IThemeService;
            themeService?.ApplyTheme(themeService.CurrentTheme);
            _window.Activate();

            Log.Information("Application launched successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            throw;
        }
    }

    private static void ConfigureSerilog()
    {
        // Create logs directory in packaged app's LocalFolder
        var logsDirectory = Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs");
        Directory.CreateDirectory(logsDirectory);

        var logFilePath = Path.Combine(logsDirectory, "winwallhaven.log");

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.Console()
            .WriteTo.File(logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                rollOnFileSizeLimit: true)
            .CreateLogger();

        Log.Information("Application starting up. Logs will be written to: {LogPath}", logFilePath);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Add Serilog as the logging provider
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        services.AddHttpClient<IWallpaperService, WindowsWallpaperService>();
        services.AddHttpClient<IWallhavenApiClient, WallhavenApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IHistoryService, LocalHistoryService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<LatestViewModel>();
        services.AddSingleton<ToplistViewModel>();
        services.AddSingleton<RandomViewModel>();
        services.AddSingleton<HistoryViewModel>();
    }
}