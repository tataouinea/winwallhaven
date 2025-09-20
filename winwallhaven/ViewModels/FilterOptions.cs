namespace winwallhaven.ViewModels;

public sealed class FilterOptions : ViewModelBase
{
    private bool _categoryAnime = true;

    // Categories (General, Anime, People) -> 3-bit string
    private bool _categoryGeneral = true;
    private bool _categoryPeople = true;
    private bool _purityNsfw;

    // Purity (SFW, Sketchy, NSFW) -> 3-bit string
    private bool _puritySfw = true;
    private bool _puritySketchy;

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

    public bool PuritySketchy
    {
        get => _puritySketchy;
        set => SetProperty(ref _puritySketchy, value);
    }

    public bool PurityNsfw
    {
        get => _purityNsfw;
        set => SetProperty(ref _purityNsfw, value);
    }

    public string GetCategoriesParam()
    {
        // API expects a 3-char bitstring: General, Anime, People
        return BoolToChar(CategoryGeneral).ToString() + BoolToChar(CategoryAnime) + BoolToChar(CategoryPeople);
    }

    public string GetPurityParam()
    {
        // API expects a 3-char bitstring: SFW, Sketchy, NSFW
        return BoolToChar(PuritySfw).ToString() + BoolToChar(PuritySketchy) + BoolToChar(PurityNsfw);
    }

    private static char BoolToChar(bool v)
    {
        return v ? '1' : '0';
    }
}