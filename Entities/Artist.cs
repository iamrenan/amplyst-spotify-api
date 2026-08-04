using amplyst_spotify_api.Common;

namespace amplyst_spotify_api.Entities;

public class Artist : Auditable
{
    public required string Name { get; set; }
    /// <summary>
    /// An image URL for the artist, if available. Typically the largest image available from Spotify's API. If no image is available, this property will be null.
    /// </summary>
    public string? ImageUrl { get; init; }
    public string? SpotifyArtistId { get; init; }
    public string? SpotifyArtistUri { get; init; }

    /// <summary>
    /// Used by Spotify. Local file artists have no Spotify id or uri, so their last resort is to be matched by name.
    /// </summary>
    public string GetMatchKey() => SpotifyArtistId ?? SpotifyArtistUri ?? $"local::{Name}";
}