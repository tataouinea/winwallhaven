using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
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
            _logger.LogInformation("wallhaven.cc API client configured with API key (masked) and base {Base}",
                _http.BaseAddress);
        }
        else
        {
            _logger.LogInformation("wallhaven.cc API client created without API key. Base {Base}", _http.BaseAddress);
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
        // Enforce SFW-only for Microsoft Store compliance regardless of incoming value
        var purityParam = "100";
        Add("purity", purityParam);
        Add("sorting", query.Sorting);
        Add("order", query.Order);
        Add("page", query.Page?.ToString());
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
            query.Query, query.Categories, purityParam);

        try
        {
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var status = resp.StatusCode;
                var reason = GetStatusDescription(status);
                _logger.LogWarning("wallhaven.cc search request failed with status {Status} ({Reason}) for {Url}", status, reason, url);
                // Include StatusCode so UI can read it; do not include body.
                throw new HttpRequestException($"Request failed with status {(int)status} ({reason}).", null, status);
            }

            var content = await resp.Content.ReadAsStringAsync(ct);
            var json = JObject.Parse(content);
            var data = (JArray)json["data"]!;
            var list = new List<Wallpaper>(data.Count);
            foreach (var item in data) list.Add(ParseWallpaper((JObject)item));
            var meta = json["meta"] as JObject;
            var currentPage = (int?)meta?["current_page"] ?? query.Page ?? 1;
            var lastPage = (int?)meta?["last_page"];
            var total = (int?)meta?["total"];
            _logger.LogInformation("Search returned {Count} wallpapers for query {Query} (Page {Page}/{Last})", list.Count,
                query.Query, currentPage, lastPage);
            return new WallpaperSearchResult(list, currentPage, lastPage, total);
        }
        catch (OperationCanceledException oce) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(oce, "wallhaven.cc search request timed out");
            throw new TimeoutException("The request timed out.", oce);
        }
        catch (HttpRequestException hre)
        {
            _logger.LogError(hre, "HTTP error while contacting wallhaven.cc");
            throw; // Preserve as HTTP error for UI handling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while contacting wallhaven.cc API");
            throw new Exception("The request failed due to a connectivity or unexpected error.", ex);
        }
    }

    public async Task<Wallpaper?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching wallpaper by id {Id}", id);
        try
        {
            using var resp = await _http.GetAsync($"wallpaper/{id}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogInformation("wallhaven.cc wallpaper id {Id} not found. Status {Status}", id, resp.StatusCode);
                return null;
            }

            var json = JObject.Parse(await resp.Content.ReadAsStringAsync(ct));
            var data = (JObject)json["data"]!;
            _logger.LogTrace("Wallpaper raw json length {Length}", json.ToString().Length);
            return ParseWallpaper(data);
        }
        catch (OperationCanceledException oce) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(oce, "wallhaven.cc GetById request timed out");
            throw new TimeoutException("The request timed out.", oce);
        }
        catch (HttpRequestException hre)
        {
            _logger.LogError(hre, "HTTP error while contacting wallhaven.cc");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while contacting wallhaven.cc API");
            throw new Exception("The request failed due to a connectivity or unexpected error.", ex);
        }
    }

    public async Task<IReadOnlyList<string>> GetTagSuggestionsAsync(string partial, CancellationToken ct = default)
    {
        await Task.Yield();
        _logger.LogDebug("Tag suggestions requested for partial '{Partial}' (not implemented)", partial);
        return Array.Empty<string>();
    }

    private static string GetStatusDescription(HttpStatusCode status)
    {
        return status switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.Forbidden => "Forbidden",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.RequestTimeout => "Request Timeout",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            HttpStatusCode.BadGateway => "Bad Gateway",
            HttpStatusCode.ServiceUnavailable => "Service Unavailable",
            HttpStatusCode.GatewayTimeout => "Gateway Timeout",
            _ => status.ToString()
        };
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
            ColorsHex = string.Join(',', obj["colors"] as JArray ?? new JArray()),
            Ratio = (string?)obj["ratio"],
            Views = (int?)obj["views"],
            CreatedAt = (string?)obj["created_at"],
            UploaderUsername = null,
            UploaderAvatarUrl = null,
            Favorites = (int?)obj["favorites"]
        };
    }
}