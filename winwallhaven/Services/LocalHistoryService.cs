using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using winwallhaven.Core.Models;

namespace winwallhaven.Services;

internal sealed class LocalHistoryService : IHistoryService
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private Dictionary<string, PersistedEntry> _entries = new();

    public LocalHistoryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "winwallhaven", "history");
        Directory.CreateDirectory(dir);
        _dbPath = Path.Combine(dir, "history.json");
        _ = LoadAsync();
    }

    public async Task RecordAsync(Wallpaper wallpaper, HistoryAction action, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await LoadAsyncLocked().ConfigureAwait(false);
            if (_entries.TryGetValue(wallpaper.Id, out var existing))
            {
                existing.LastSetAt = DateTimeOffset.UtcNow;
                existing.LastAction = action;
            }
            else
            {
                _entries[wallpaper.Id] = new PersistedEntry
                {
                    Wallpaper = wallpaper,
                    LastSetAt = DateTimeOffset.UtcNow,
                    LastAction = action
                };
            }

            await SaveAsyncLocked().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(string wallpaperId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await LoadAsyncLocked().ConfigureAwait(false);
            if (_entries.Remove(wallpaperId))
                await SaveAsyncLocked().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _entries.Clear();
            await SaveAsyncLocked().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await LoadAsyncLocked().ConfigureAwait(false);
            return _entries.Values
                .OrderByDescending(e => e.LastSetAt)
                .Select(e => new HistoryEntry(e.Wallpaper, e.LastSetAt, e.LastAction))
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task LoadAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await LoadAsyncLocked().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task LoadAsyncLocked()
    {
        if (!File.Exists(_dbPath))
        {
            _entries = new Dictionary<string, PersistedEntry>();
            return;
        }

        await using var fs = File.OpenRead(_dbPath);
        var model = await JsonSerializer.DeserializeAsync<FileModel>(fs, _jsonOptions) ??
                    new FileModel { Items = new List<PersistedEntry>() };
        _entries = model.Items.ToDictionary(i => i.Wallpaper.Id);
    }

    private async Task SaveAsyncLocked()
    {
        var dir = Path.GetDirectoryName(_dbPath)!;
        Directory.CreateDirectory(dir);
        var tmp = _dbPath + ".tmp";
        var model = new FileModel { Items = _entries.Values.ToList(), Version = 1 };
        await using (var fs = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(fs, model, _jsonOptions).ConfigureAwait(false);
        }

        File.Copy(tmp, _dbPath, true);
        File.Delete(tmp);
    }

    private sealed class PersistedEntry
    {
        public required Wallpaper Wallpaper { get; init; }
        public required DateTimeOffset LastSetAt { get; set; }
        public required HistoryAction LastAction { get; set; }
    }

    private sealed class FileModel
    {
        public required List<PersistedEntry> Items { get; init; }
        public int Version { get; init; } = 1;
    }
}