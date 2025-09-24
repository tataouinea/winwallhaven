using System;
using System.IO;
using System.Text.Json;
using Windows.Storage;
using Microsoft.UI.Xaml;

namespace winwallhaven.Services;

public class ThemeService : IThemeService
{
    private readonly string _settingsPath;

    public ThemeService()
    {
        var dir = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");

        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var model = JsonSerializer.Deserialize<SettingsModel>(json);
                if (model is not null && Enum.TryParse<AppTheme>(model.AppTheme, out var parsed)) CurrentTheme = parsed;
            }
        }
        catch
        {
            // Ignore any corrupt settings and fall back to default
        }
    }

    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    public void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;

        // Persist setting
        try
        {
            var model = new SettingsModel { AppTheme = theme.ToString() };
            var json = JsonSerializer.Serialize(model);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Best-effort persistence; ignore IO errors
        }

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

    private sealed class SettingsModel
    {
        public string AppTheme { get; set; } = "System";
    }
}