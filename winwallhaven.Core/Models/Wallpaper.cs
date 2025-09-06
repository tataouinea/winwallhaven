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
}