using amplyst_spotify_api.Entities;
using amplyst_spotify_api.Models.Spotify;

namespace amplyst_spotify_api.Mapping;

public static class PlaylistMappingExtensions
{
    public static Playlist ToEntity(this SimplifiedPlaylist playlist)
    {
        return new Playlist
        {
            Id = Guid.NewGuid(),
            Name = playlist.Name,

            SpotifyPlaylistId = playlist.Id,
            SpotifySnapshotId = playlist.SnapshotId,
            SpotifyPlaylistUri = playlist.Uri,
            SpotifyOwnerId = playlist.Owner?.Id,
        };
    }


}