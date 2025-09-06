using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using winwallhaven.ViewModels;

namespace winwallhaven.Pages;

public sealed partial class SearchPage : Page
{
    public SearchPage()
    {
        InitializeComponent();
        // Resolve VM from DI
        DataContext = App.Services.GetRequiredService<SearchViewModel>();
    }
}