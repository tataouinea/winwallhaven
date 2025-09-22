namespace winwallhaven.ViewModels;

public sealed class FilterOptions : ViewModelBase
{
    private bool _categoryAnime;

    // Categories (General, Anime, People) -> 3-bit string
    private bool _categoryGeneral = true;
    private bool _categoryPeople;
    private int? _minHeight;
    private int? _minWidth;

    // Purity (SFW-only for Microsoft Store compliance)
    private bool _puritySfw = true;

    public bool CategoryGeneral
    {
        get => _categoryGeneral;
        set => SetProperty(ref _categoryGeneral, value);
    }

    public bool CategoryAnime
    {
        get => _categoryAnime;
        set => SetProperty(ref _categoryAnime, value);
    }

    public bool CategoryPeople
    {
        get => _categoryPeople;
        set => SetProperty(ref _categoryPeople, value);
    }

    public bool PuritySfw
    {
        get => _puritySfw;
        set => SetProperty(ref _puritySfw, value);
    }

    // Minimum resolution (Wallhaven 'atleast' parameter)
    public int? MinWidth
    {
        get => _minWidth;
        set => SetProperty(ref _minWidth, value);
    }

    public int? MinHeight
    {
        get => _minHeight;
        set => SetProperty(ref _minHeight, value);
    }

    public void ClearMinResolution()
    {
        _ = SetProperty(ref _minWidth, null, nameof(MinWidth));
        _ = SetProperty(ref _minHeight, null, nameof(MinHeight));
    }

    public string GetCategoriesParam()
    {
        // API expects a 3-char bitstring: General, Anime, People
        return BoolToChar(CategoryGeneral).ToString() + BoolToChar(CategoryAnime) + BoolToChar(CategoryPeople);
    }

    public string GetPurityParam()
    {
        // API expects a 3-char bitstring: SFW, Sketchy, NSFW
        // For Store submission, enforce SFW-only regardless of UI state.
        return "100";
    }

    private static char BoolToChar(bool v)
    {
        return v ? '1' : '0';
    }
}