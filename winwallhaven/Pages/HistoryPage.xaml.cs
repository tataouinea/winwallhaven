using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using winwallhaven.ViewModels;

namespace winwallhaven.Pages;

public sealed partial class HistoryPage : Page
{
    public HistoryPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<HistoryViewModel>();
        _ = (DataContext as HistoryViewModel)!.LoadAsync();
    }
}