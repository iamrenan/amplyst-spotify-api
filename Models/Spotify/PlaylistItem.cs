namespace amplyst_spotify_api.Models.Spotify;

public record PlaylistItem
{
    public DateTime? AddedAt { get; init; }
    public PlaylistOwner? AddedBy { get; init; }
    public required bool IsLocal { get; init; }
    public required TrackOrEpisode? Item { get; init; }
}

public record PlaylistOwner(ExternalUrls ExternalUrls, string Href, string Id, string Type, string Uri);
