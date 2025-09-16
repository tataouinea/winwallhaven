using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using winwallhaven.Core.Models;

namespace winwallhaven.Services;

public enum HistoryAction
{
    Wallpaper,
    LockScreen
}

public sealed record HistoryEntry(Wallpaper Wallpaper, DateTimeOffset LastSetAt, HistoryAction LastAction);

public interface IHistoryService
{
    Task RecordAsync(Wallpaper wallpaper, HistoryAction action, CancellationToken ct = default);
    Task RemoveAsync(string wallpaperId, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HistoryEntry>> GetAllAsync(CancellationToken ct = default);
}