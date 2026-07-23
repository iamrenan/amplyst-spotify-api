using System.Text.Json.Serialization;

namespace amplyst_spotify_api.Models;

internal record SimplifiedPlaylist
{
    public required bool Collaborative { get; init; }
    public required string Description { get; init; }
    public required ExternalUrls ExternalUrls { get; init; }
    public required string Href { get; init; }
    public required string Id { get; init; }
    public required List<Image>? Images { get; init; }
    public required string Name { get; init; }
    public required Owner Owner { get; init; }

    [JsonPropertyName("public")]
    public required bool IsPublic { get; init; }
    public required string SnapshotId { get; init; }
    public required Item Items { get; init; }
    public Item? Tracks { get; init; }
    public string? PrimaryColor { get; init; }
}

internal sealed record ExternalUrls(
    string? Spotify
);
internal sealed record Image(
    string Url,
    int? Height,
    int? Width
);
internal sealed record Owner(
    string? DisplayName,
    string Type,
    string Id,
    string Uri,
    string Href,
    ExternalUrls ExternalUrls
);
internal sealed record Item(
    string Href,
    int Total
);