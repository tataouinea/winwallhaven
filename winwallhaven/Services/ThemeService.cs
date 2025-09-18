using System;
using Windows.Storage;
using Microsoft.UI.Xaml;

namespace winwallhaven.Services;

public class ThemeService : IThemeService
{
    private const string SettingsKey = "AppTheme";
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    public ThemeService()
    {
        // Load saved preference
        if (_localSettings.Values.TryGetValue(SettingsKey, out var value) && value is string s)
            if (Enum.TryParse<AppTheme>(s, out var parsed))
                CurrentTheme = parsed;
    }

    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    public void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        _localSettings.Values[SettingsKey] = theme.ToString();

        var window = App.MainAppWindow;
        if (window is null) return;

        var root = window.Content as FrameworkElement;
        if (root is null) return;

        // System default means "Default" which follows system
        root.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }
}