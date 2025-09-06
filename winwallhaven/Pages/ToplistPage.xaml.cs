using winwallhaven.ViewModels;

namespace winwallhaven.Pages;

public sealed partial class ToplistPage : BrowsingPage
{
    public ToplistPage()
    {
        InitializeComponent();
        ViewModelType = typeof(ToplistViewModel);
    }
}
