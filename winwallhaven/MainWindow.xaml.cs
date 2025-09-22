using System;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using winwallhaven.Pages;
using winwallhaven.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winwallhaven;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TryEnableTallTitleBar();
    }

    private void NavView_OnLoaded(object sender, RoutedEventArgs e)
    {
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            RootFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        switch (tag)
        {
            case "Latest":
                RootFrame.Navigate(typeof(BrowsingPage), typeof(LatestViewModel));
                break;
            case "Toplist":
                RootFrame.Navigate(typeof(BrowsingPage), typeof(ToplistViewModel));
                break;
            case "Random":
                RootFrame.Navigate(typeof(BrowsingPage), typeof(RandomViewModel));
                break;
            case "History":
                RootFrame.Navigate(typeof(HistoryPage));
                break;
            case "Search":
                RootFrame.Navigate(typeof(SearchPage));
                break;
            case "About":
                OpenUrl("https://github.com/tataouinea/winwallhaven");
                break;
            case "ReportBug":
                OpenUrl("https://github.com/tataouinea/winwallhaven/issues");
                break;
            case "Feedback":
                OpenUrl("https://github.com/tataouinea/winwallhaven/discussions");
                break;
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void TryEnableTallTitleBar()
    {
        try
        {
            // Get the AppWindow for this Window and opt into the tall system title bar when available.
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // Extend content into title bar and request the tall system title bar height.
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

                // Let the Window manage hit-testing for the caption buttons and drag behavior.
                if (AppTitleBar != null) SetTitleBar(AppTitleBar);
            }
        }
        catch
        {
            // Best-effort only; ignore if not supported on this OS or SDK.
        }
    }
}