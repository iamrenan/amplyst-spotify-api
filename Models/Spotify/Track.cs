namespace amplyst_spotify_api.Models.Spotify;

public record Track : TrackOrEpisode
{
     public SimplifiedAlbum? Album { get; init; }
     public required SimplifiedArtist[] Artists { get; init; }
     public int DiscNumber { get; init; }
     public int DurationMs { get; init; }
     public bool Explicit { get; init; }
     public ExternalIds? ExternalIds { get; init; }
     public bool? IsPlayable { get; init; }
     public Restriction? Restrictions { get; init; }
     public required string Name { get; init; }
     public int TrackNumber { get; init; }
     public bool IsLocal { get; init; }
}

public record SimplifiedAlbum(
     string? AlbumType,
     int TotalTracks,
     ExternalUrls? ExternalUrls,
     string? Href,
     string? Id,
     Image[] Images,
     string Name,
     string ReleaseDate,
     string ReleaseDatePrecision,
     Restriction? Restrictions,
     string Type,
     string? Uri,
     SimplifiedArtist[]? Artists
);

public record ExternalIds(string? Isrc, string? Ean, string? Upc);

public record Restriction(string? Reason);