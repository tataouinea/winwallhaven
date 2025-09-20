using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using winwallhaven.Services;
using winwallhaven.ViewModels;

namespace winwallhaven.Pages;

public partial class BrowsingPage : Page
{
    public BrowsingPage()
    {
        InitializeComponent();
    }

    public Type? ViewModelType { get; set; }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is Type vmType)
        {
            ViewModelType = vmType;
            DataContext = App.Services.GetRequiredService(vmType);
        }

        if (DataContext is BrowsingViewModelBase vm) await vm.LoadAsync();
    }

    private void MinResolutionFlyout_Opening(object sender, object e)
    {
        if (DataContext is not BrowsingViewModelBase vm) return;
        if (vm.Filters.MinWidth == null || vm.Filters.MinHeight == null)
            if (ScreenResolutionHelper.TryGetCurrentMonitorResolution(out var w, out var h))
            {
                vm.Filters.MinWidth = w;
                vm.Filters.MinHeight = h;
            }
    }

    private void UseCurrentScreen_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BrowsingViewModelBase vm) return;
        if (ScreenResolutionHelper.TryGetCurrentMonitorResolution(out var w, out var h))
        {
            vm.Filters.MinWidth = w;
            vm.Filters.MinHeight = h;
            vm.ApplyFiltersCommand?.Execute(null);
        }
    }

    private void ClearMinResolution_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BrowsingViewModelBase vm) return;
        vm.Filters.ClearMinResolution();
        vm.ApplyFiltersCommand?.Execute(null);
    }

    private void PresetResolution_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BrowsingViewModelBase vm) return;
        if (sender is Button b && b.Tag is string tag && TryParseRes(tag, out var w, out var h))
        {
            vm.Filters.MinWidth = w;
            vm.Filters.MinHeight = h;
            vm.ApplyFiltersCommand?.Execute(null);
        }
    }

    private void ApplyMinResolution_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BrowsingViewModelBase vm) return;
        vm.ApplyFiltersCommand?.Execute(null);
    }

    private static bool TryParseRes(string s, out int w, out int h)
    {
        w = h = 0;
        var parts = s.Split('x', '×');
        if (parts.Length != 2) return false;
        return int.TryParse(parts[0], out w) && int.TryParse(parts[1], out h);
    }
}