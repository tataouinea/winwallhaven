using System;

namespace winwallhaven.Core.Models;

public sealed class Wallpaper
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public required string Path { get; init; }
    public required string Thumb { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string FileType { get; init; }
    public required long FileSize { get; init; }
    public required string Category { get; init; }
    public required string Purity { get; init; }
    public required string[] Tags { get; init; }
    public string? ColorsHex { get; init; }

    // Extended metadata
    public string? Ratio { get; init; }
    public int? Views { get; init; }
    public string? CreatedAt { get; init; } // ISO timestamp from API
    public string? UploaderUsername { get; init; }
    public string? UploaderAvatarUrl { get; init; }
    public int? Favorites { get; init; }

    public string? UserProfileUrl => string.IsNullOrWhiteSpace(UploaderUsername)
        ? null
        : $"https://wallhaven.cc/user/{UploaderUsername}";

    public string? UploaderInitial => string.IsNullOrWhiteSpace(UploaderUsername)
        ? null
        : UploaderUsername![0].ToString().ToUpperInvariant();

    public string Resolution => Width > 0 && Height > 0 ? $"{Width}x{Height}" : string.Empty;

    public string[] Colors => string.IsNullOrWhiteSpace(ColorsHex)
        ? Array.Empty<string>()
        : ColorsHex.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}