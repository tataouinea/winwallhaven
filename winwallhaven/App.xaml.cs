using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using winwallhaven.Core.Services;
using winwallhaven.Core.Wallpapers;
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
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        _window = new MainWindow();
        MainAppWindow = _window;
        _window.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddHttpClient<IWallpaperService, WindowsWallpaperService>();
        services.AddHttpClient<IWallhavenApiClient, WallhavenApiClient>();
        services.AddSingleton<SearchViewModel>();
    }
}