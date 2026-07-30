namespace amplyst_spotify_api.Models.Spotify;

public record SimplifiedArtist(
    string Name,
    string Href,
    string Id,
    string Type,
    string Uri,
    ExternalUrls ExternalUrls
);