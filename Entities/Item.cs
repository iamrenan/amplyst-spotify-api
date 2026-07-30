
using amplyst_spotify_api.Common;

namespace amplyst_spotify_api.Entities;

/// <summary>
/// Represents a track or an episode in the Spotify catalog.
/// </summary>
public class Item : Auditable
{
    public required string Name { get; set; }
    public required List<Artist> Artists { get; set; }
    /// <summary>
    /// Spotify unique identifier for the track, be it a track or episode.
    /// </summary>
    public string? SpotifyItemId { get; init; }
    public string? AlbumName { get; init; }
    public int DiscNumber { get; init; }
    public int? DurationMs { get; init; }
    public bool? Explicit { get; init; }
    public string? ReleaseDate { get; init; }
    public string? ReleaseDatePrecision { get; init; }
    /// <summary>
    /// The International Standard Recording Code (ISRC) for the track, if available. This is a unique identifier used for music tracks and is typically assigned by the record label. It may be null if the ISRC is not available for the track.
    /// </summary>
    public string? ISRC { get; init; }

    /// <summary>
    /// The International Article Number (EAN) for the track, if available. This is a unique identifier used for commercial products, including music tracks. It may be null if the EAN is not available for the track.
    /// </summary>
    public string? EAN { get; init; }
    /// <summary>
    /// The Universal Product Code (UPC) for the track, if available. This is a unique identifier used for commercial products, including music tracks. It may be null if the UPC is not available for the track.
    /// </summary>
    public string? UPC { get; init; }
}