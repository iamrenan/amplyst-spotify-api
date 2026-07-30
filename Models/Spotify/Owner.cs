namespace amplyst_spotify_api.Models.Spotify;

public sealed record Owner(
    string? DisplayName,
    string Type,
    string Id,
    string Uri,
    string Href,
    ExternalUrls ExternalUrls
);
