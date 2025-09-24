using System;
using System.Diagnostics;
using Windows.System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
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

        // Monitor navigation to keep the title search in sync with the current page
        RootFrame.Navigated += RootFrame_Navigated;
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
            UpdateTitleSearchVisibility(false);
            return;
        }

        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        switch (tag)
        {
            case "Latest":
                RootFrame.Navigate(typeof(BrowsingPage), typeof(LatestViewModel));
                UpdateTitleSearchVisibility(false);
                break;
            case "Toplist":
                RootFrame.Navigate(typeof(BrowsingPage), typeof(ToplistViewModel));
                UpdateTitleSearchVisibility(false);
                break;
            case "Random":
                RootFrame.Navigate(typeof(BrowsingPage), typeof(RandomViewModel));
                UpdateTitleSearchVisibility(false);
                break;
            case "History":
                RootFrame.Navigate(typeof(HistoryPage));
                UpdateTitleSearchVisibility(false);
                break;
            case "Search":
                RootFrame.Navigate(typeof(SearchPage));
                UpdateTitleSearchVisibility(true);
                // Attempt to bind DataContext to SearchViewModel for the title search box
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (RootFrame.Content is FrameworkElement fe && fe.DataContext is SearchViewModel svm)
                        TitleSearchHost.DataContext = svm;
                });
                break;
        }
    }

    private void RootFrame_Navigated(object sender, NavigationEventArgs e)
    {
        var isSearch = e.Content is SearchPage;
        UpdateTitleSearchVisibility(isSearch);
        if (isSearch && e.Content is FrameworkElement fe && fe.DataContext is SearchViewModel svm)
            TitleSearchHost.DataContext = svm;
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

    private void UpdateTitleSearchVisibility(bool visible)
    {
        if (TitleSearchHost == null) return;
        TitleSearchHost.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TitleSearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
            if (TitleSearchHost?.DataContext is SearchViewModel vm && vm.SearchCommand.CanExecute(null))
            {
                vm.SearchCommand.Execute(null);
                e.Handled = true;
            }
    }

    private void TitleSearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (TitleSearchHost?.DataContext is SearchViewModel vm && vm.SearchCommand.CanExecute(null))
            vm.SearchCommand.Execute(null);
    }
}