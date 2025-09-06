using winwallhaven.ViewModels;

namespace winwallhaven.Pages;

public sealed partial class LatestPage : BrowsingPage
{
    public LatestPage()
    {
        InitializeComponent();
        ViewModelType = typeof(LatestViewModel);
    }
}
