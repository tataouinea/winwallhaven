using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace winwallhaven.Services;

/// <summary>
///     Shows a warning dialog when the lock screen mode is not Picture so users know to switch.
///     Persists suppression in ApplicationData.Current.LocalSettings.
/// </summary>
public static class LockScreenWarningService
{
    private const string SuppressKey = "SuppressLockScreenModeWarning";

    public static bool IsSuppressed()
    {
        var settings = ApplicationData.Current.LocalSettings;
        return settings.Values[SuppressKey] as bool? == true;
    }

    public static void SetSuppressed(bool value)
    {
        ApplicationData.Current.LocalSettings.Values[SuppressKey] = value;
    }

    public static void ResetSuppression()
    {
        // Remove so future checks treat it as not suppressed
        ApplicationData.Current.LocalSettings.Values.Remove(SuppressKey);
    }

    public static async Task MaybeShowAsync()
    {
        if (IsSuppressed()) return;

        var cb = new CheckBox { Content = "Don't show again" };
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(new TextBlock
        {
            Text =
                "Your selected image has been applied to the lock screen background. However, Windows won't display it while the lock screen mode is set to Windows Spotlight or Slideshow. To immediately see this image, open Lock screen settings and change the personalization mode to 'Picture'.",
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(cb);

        var dlg = new ContentDialog
        {
            Title = "Lock screen limitation",
            Content = stack,
            PrimaryButtonText = "Open Settings",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainAppWindow!.Content.XamlRoot
        };

        var result = await dlg.ShowAsync();
        if (cb.IsChecked == true) SetSuppressed(true);
        if (result == ContentDialogResult.Primary)
            await Launcher.LaunchUriAsync(new Uri("ms-settings:lockscreen"));
    }
}