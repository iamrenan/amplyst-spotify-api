using amplyst_spotify_api.Common;

namespace amplyst_spotify_api.Entities;

public class PlaylistItem : Auditable
{
    /// <summary>
    /// Application-level unique identifier for the playlist that this track belongs to.
    /// </summary>
    public required Guid PlaylistId { get; init; }
    /// <summary>
    /// Application-level unique identifier for the track that this playlist track represents.
    /// </summary>
    public required Guid ItemId { get; init; }
    /// <summary>
    /// Spotify unique identifier for the playlist that this track belongs to.
    /// </summary>
    public string? SpotifyPlaylistId { get; set; }
    /// <summary>
    /// Spotify unique identifier for the track that this playlist track represents.
    /// </summary>
    public string? SpotifyItemId { get; set; }
    /// <summary>
    /// Spotify unique identifier for the user that added this track to the playlist.
    /// </summary>
    public string? SpotifyAddedById { get; set; }
    /// <summary>
    /// The date and time that the track was added to the playlist in Spotify.
    /// </summary>
    public DateTime? SpotifyAddedAt { get; set; }
}