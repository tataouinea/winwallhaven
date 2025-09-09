using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using winwallhaven.Core.Models;

namespace winwallhaven.Core.Services;

public sealed class WallhavenApiClient : IWallhavenApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<WallhavenApiClient> _logger;

    public WallhavenApiClient(HttpClient http, ILogger<WallhavenApiClient> logger, string? apiKey = null)
    {
        _http = http;
        _logger = logger;
        _http.BaseAddress = new Uri("https://wallhaven.cc/api/v1/");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _logger.LogInformation("Wallhaven API client configured with API key (masked) and base {Base}",
                _http.BaseAddress);
        }
        else
        {
            _logger.LogInformation("Wallhaven API client created without API key. Base {Base}", _http.BaseAddress);
        }
    }

    public async Task<WallpaperSearchResult> SearchAsync(WallpaperSearchQuery query, CancellationToken ct = default)
    {
        var qp = new List<string>();

        void Add(string name, string? val)
        {
            if (!string.IsNullOrWhiteSpace(val)) qp.Add($"{name}={Uri.EscapeDataString(val)}");
        }

        Add("q", query.Query);
        Add("categories", query.Categories);
        Add("purity", query.Purity);
        Add("sorting", query.Sorting);
        Add("order", query.Order);
        Add("page", query.Page?.ToString());
        // Add("per_page", query.PerPage?.ToString());
        Add("atleast",
            query.AtLeastWidth.HasValue && query.AtLeastHeight.HasValue
                ? $"{query.AtLeastWidth}x{query.AtLeastHeight}"
                : null);
        Add("resolutions", query.Resolutions);
        Add("ratios", query.Ratios);
        Add("colors", query.Colors);
        Add("topRange", query.TopRange);
        Add("ai_art_filter", query.AiArtFilter);
        var url = "search" + (qp.Count > 0 ? "?" + string.Join('&', qp) : string.Empty);
        _logger.LogDebug("Issuing search request: {Url} Query={Query} Categories={Categories} Purity={Purity}", url,
            query.Query, query.Categories, query.Purity);
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Search request failed with status {Status} for {Url}", resp.StatusCode, url);
            resp.EnsureSuccessStatusCode();
        }

        var json = JObject.Parse(await resp.Content.ReadAsStringAsync(ct));
        var data = (JArray)json["data"]!;
        var list = new List<Wallpaper>(data.Count);
        foreach (var item in data) list.Add(ParseWallpaper((JObject)item));
        var meta = json["meta"] as JObject;
        var currentPage = (int?)meta?["current_page"] ?? query.Page ?? 1;
        var lastPage = (int?)meta?["last_page"];
        // var perPage = (int?)meta?["per_page"]; // keep for reference though fixed (24)
        var total = (int?)meta?["total"];
        _logger.LogInformation("Search returned {Count} wallpapers for query {Query} (Page {Page}/{Last})", list.Count,
            query.Query, currentPage, lastPage);
        return new WallpaperSearchResult(list, currentPage, lastPage, total);
    }

    public async Task<Wallpaper?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching wallpaper by id {Id}", id);
        using var resp = await _http.GetAsync($"wallpaper/{id}", ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogInformation("Wallpaper id {Id} not found. Status {Status}", id, resp.StatusCode);
            return null;
        }

        var json = JObject.Parse(await resp.Content.ReadAsStringAsync(ct));
        var data = (JObject)json["data"]!;
        _logger.LogTrace("Wallpaper raw json length {Length}", json.ToString().Length);
        return ParseWallpaper(data);
    }

    public async Task<IReadOnlyList<string>> GetTagSuggestionsAsync(string partial, CancellationToken ct = default)
    {
        await Task.Yield();
        _logger.LogDebug("Tag suggestions requested for partial '{Partial}' (not implemented)", partial);
        return Array.Empty<string>();
    }

    private static Wallpaper ParseWallpaper(JObject obj)
    {
        var tags = obj["tags"] is JArray tagsArr
            ? tagsArr.Select(t => (string?)t["name"] ?? string.Empty).Where(s => s.Length > 0).ToArray()
            : Array.Empty<string>();
        return new Wallpaper
        {
            Id = (string?)obj["id"] ?? string.Empty,
            Url = (string?)obj["url"] ?? string.Empty,
            Path = (string?)obj["path"] ?? string.Empty,
            Thumb = (string?)obj["thumbs"]?["small"] ?? string.Empty,
            Width = (int?)obj["dimension_x"] ?? 0,
            Height = (int?)obj["dimension_y"] ?? 0,
            FileType = (string?)obj["file_type"] ?? string.Empty,
            FileSize = (long?)obj["file_size"] ?? 0,
            Category = (string?)obj["category"] ?? string.Empty,
            Purity = (string?)obj["purity"] ?? string.Empty,
            Tags = tags,
            ColorsHex = string.Join(',', obj["colors"] as JArray ?? new JArray())
        };
    }
}