using amplyst_spotify_api.Entities;
using amplyst_spotify_api.Models.Spotify;

namespace amplyst_spotify_api.Mapping;

public static class ItemMappingExtensions
{
    public static Item ToItem(this TrackOrEpisode trackOrEpisode, bool isLocal)
    {
        return trackOrEpisode switch
        {
            Track track => new Item
            {
                Id = Guid.NewGuid(),
                Name = $"{string.Join(", ", (track.Artists ?? []).Select(a => a.Name))} - {track.Name}",
                Artists = [
                    ..(track.Artists ?? []).Select(a => new Artist
                        {
                            Id = Guid.NewGuid(),
                            SpotifyArtistUri = a.Uri,
                            Name = a.Name,
                            SpotifyArtistId = a.Id,
                        }
                    )
                ],
                SpotifyItemId = track.Id,
                SpotifyItemUri = track.Uri,
                IsLocal = isLocal,
                AlbumName = track.Album?.Name,
                DiscNumber = track.DiscNumber,
                DurationMs = track.DurationMs,
                Explicit = track.Explicit,
                EAN = track.ExternalIds?.Ean,
                ISRC = track.ExternalIds?.Isrc,
                UPC = track.ExternalIds?.Upc,
                ReleaseDate = track.Album?.ReleaseDate,
                ReleaseDatePrecision = track.Album?.ReleaseDatePrecision,
            },
            Episode episode => new Item
            {
                Id = Guid.NewGuid(),
                Name = episode.Name,
                Artists = [
                     new Artist
                    {
                        Id = Guid.NewGuid(),
                        SpotifyArtistUri = episode.Show?.Uri,
                        Name = episode.Show?.Name ?? "Unknown Show",
                        SpotifyArtistId = episode.Show?.Id,
                    }
                ],
                SpotifyItemId = episode.Id,
                SpotifyItemUri = episode.Uri,
                IsLocal = isLocal,
                AlbumName = episode.Show?.Name,
                DiscNumber = 1,
                DurationMs = episode.DurationMs,
                Explicit = episode.Explicit,
                ReleaseDate = episode.ReleaseDate,
                ReleaseDatePrecision = episode.ReleaseDatePrecision,
            },
            _ => throw new InvalidOperationException($"Unsupported item type: {trackOrEpisode.GetType().Name}"),
        };
    }
    public static Entities.PlaylistItem ToPlaylistItem(this Models.Spotify.PlaylistItem playlistItem, Playlist playlist, Item item, string createdBy)
    {
        return new Entities.PlaylistItem
        {
            Id = Guid.NewGuid(),
            PlaylistId = playlist.Id,
            ItemId = item.Id,

            SpotifyPlaylistId = playlist.SpotifyPlaylistId,
            SpotifyItemId = item.SpotifyItemId,
            SpotifyItemUri = item.SpotifyItemUri,
            SpotifyAddedAt = playlistItem.AddedAt,
            SpotifyAddedById = playlistItem.AddedBy?.Id,

            CreatedBy = createdBy,
        };
    }
}