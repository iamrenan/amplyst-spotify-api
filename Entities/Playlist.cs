
using amplyst_spotify_api.Common;

namespace amplyst_spotify_api.Entities;

public class Playlist : Auditable
{
    public required string Name { get; set; }
    /// <summary>
    /// The Spotify user ID of the owner of the playlist. This is used to identify the playlist in Spotify's API.
    /// Might be null or anonymized in a few scenarios - For example, if:
    ///     - The user has deleted their account;
    ///     - The user asked to delete their account (per privacy laws);
    ///     - The playlist is a collaborative playlist and the owner has left the playlist.
    /// </summary>
    public string? UserId { get; init; }
    public string? SpotifyPlaylistId { get; init; }
    public string? SpotifySnapshotId { get; set; }
    public string? SpotifyPlaylistUri { get; init; }
}