using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
}