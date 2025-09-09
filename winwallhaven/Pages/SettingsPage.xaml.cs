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

    // Removed mode detection functionality.
}