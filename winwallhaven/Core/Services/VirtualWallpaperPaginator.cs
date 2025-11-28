using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using winwallhaven.Core.Models;

namespace winwallhaven.Core.Services;

/// <summary>
///     Aggregates several underlying Wallhaven API pages (24 items each) into a larger logical page.
/// </summary>
public sealed class VirtualWallpaperPaginator
{
    private readonly IWallhavenApiClient _api;
    private readonly int _apiPageSize = 24; // per docs
    private readonly int _logicalPageSize; // e.g. 96 (must be multiple of 24)

    public VirtualWallpaperPaginator(IWallhavenApiClient api, int logicalPageSize = 96)
    {
        if (logicalPageSize % 24 != 0)
            throw new ArgumentException("Logical page size must be a multiple of 24", nameof(logicalPageSize));
        _api = api;
        _logicalPageSize = logicalPageSize;
    }

    public async Task<VirtualWallpaperPage> GetAsync(Func<int, WallpaperSearchQuery> queryFactory, int logicalPage,
        CancellationToken ct = default)
    {
        if (logicalPage < 1) logicalPage = 1;
        // Determine which underlying API pages we need.
        var pagesPerLogical = _logicalPageSize / _apiPageSize; // e.g. 4
        var firstApiPage = (logicalPage - 1) * pagesPerLogical + 1;
        var aggregate = new List<Wallpaper>(_logicalPageSize);
        int? lastApiPage = null;
        var failures = 0;
        var successes = 0;
        Exception? lastException = null;
        for (var i = 0; i < pagesPerLogical; i++)
        {
            var apiPage = firstApiPage + i;
            var q = queryFactory(apiPage);
            try
            {
                var result = await _api.SearchAsync(q, ct);
                aggregate.AddRange(result.Items);
                successes++;
                lastApiPage = result.LastPage; // use meta from final call; they should be same anyway
                if (result.LastPage.HasValue && apiPage >= result.LastPage.Value)
                    // Reached the real end; stop early.
                    break;
            }
            catch (Exception ex)
            {
                failures++;
                lastException = ex; // remember last specific failure (HTTP status, timeout, etc.)
                // swallow and continue; if all fail we'll surface failure below
            }
        }

        // Compute virtual last page from total if available.
        int? virtualLast = null;
        if (lastApiPage.HasValue)
        {
            // If API gives total per-page not exposed; we approximate.
            // Without total we keep null so navigation can continue until an empty fetch triggers stop.
        }

        if (successes == 0 || aggregate.Count == 0)
        {
            // All API calls failed or no items were retrieved for this logical page.
            // Propagate the last specific exception if available so callers can present meaningful errors.
            if (lastException != null) throw lastException;
            throw new InvalidOperationException("Failed to retrieve items for the requested page.");
        }

        return new VirtualWallpaperPage(aggregate, logicalPage, virtualLast, _logicalPageSize);
    }
}

public sealed record VirtualWallpaperPage(
    IReadOnlyList<Wallpaper> Items,
    int LogicalPage,
    int? LastLogicalPage,
    int PageSize);