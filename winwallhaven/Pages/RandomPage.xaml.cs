using winwallhaven.ViewModels;

namespace winwallhaven.Pages;

public sealed partial class RandomPage : BrowsingPage
{
    public RandomPage()
    {
        InitializeComponent();
        ViewModelType = typeof(RandomViewModel);
    }
}
