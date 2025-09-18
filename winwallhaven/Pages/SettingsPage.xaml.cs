using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using winwallhaven.Services;

namespace winwallhaven.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly ILogger? _logger;

    public SettingsPage()
    {
        InitializeComponent();
        _logger = App.Services.GetService(typeof(ILogger<SettingsPage>)) as ILogger<SettingsPage>;
        UpdateLockScreenWarningStatus();
        InitializeThemeSelector();
    }

    private void UpdateLockScreenWarningStatus()
    {
        if (LockScreenWarningStatusText == null) return;
        var suppressed = LockScreenWarningService.IsSuppressed();
        LockScreenWarningStatusText.Text = $"Lock screen warning suppressed: {suppressed}.";
    }

    private void OnResetLockScreenWarning(object sender, RoutedEventArgs e)
    {
        LockScreenWarningService.ResetSuppression();
        UpdateLockScreenWarningStatus();
    }

    private void InitializeThemeSelector()
    {
        try
        {
            var themeService = App.Services.GetService(typeof(IThemeService)) as IThemeService;
            if (themeService == null || ThemeComboBox == null) return;

            ThemeComboBox.SelectedIndex = themeService.CurrentTheme switch
            {
                AppTheme.System => 0,
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0
            };
        }
        catch
        {
            /* ignore */
        }
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var themeService = App.Services.GetService(typeof(IThemeService)) as IThemeService;
        if (themeService == null || ThemeComboBox == null) return;

        var index = ThemeComboBox.SelectedIndex;
        var theme = index switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.System
        };
        themeService.ApplyTheme(theme);
    }
}