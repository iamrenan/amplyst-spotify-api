using System.Text.Json.Serialization;

namespace amplyst_spotify_api.Models.Spotify;

public record SimplifiedPlaylist
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
    public required SimplifiedPlaylistItem Items { get; init; }
    public SimplifiedPlaylistItem? Tracks { get; init; }
    public string? PrimaryColor { get; init; }
}
