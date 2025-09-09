using winwallhaven.Core.Models;

namespace winwallhaven.Core.Services;

public interface IWallhavenApiClient
{
    Task<WallpaperSearchResult> SearchAsync(WallpaperSearchQuery query, CancellationToken ct = default);
    Task<Wallpaper?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetTagSuggestionsAsync(string partial, CancellationToken ct = default);
}

public sealed record WallpaperSearchQuery(
    string Query,
    string? Categories = null,
    string? Purity = null,
    string? Sorting = null,
    string? Order = null,
    int? Page = null,
    int? AtLeastWidth = null,
    int? AtLeastHeight = null,
    // Comma separated list of exact resolutions (e.g. "1920x1080,2560x1440")
    string? Resolutions = null,
    string? Ratios = null,
    string? Colors = null,
    string? TopRange = null,
    string? AiArtFilter = null
);

public sealed record WallpaperSearchResult(
    IReadOnlyList<Wallpaper> Items,
    int CurrentPage,
    int? LastPage,
    int? Total
);