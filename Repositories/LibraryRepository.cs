using amplyst_spotify_api.Data;
using amplyst_spotify_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace amplyst_spotify_api.Repositories;

public interface ILibraryRepository
{
    Task<List<Playlist>> GetPlaylistsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, Item>> GetItemsByUrisAsync(IEnumerable<string> spotifyItemUris, CancellationToken cancellationToken = default);
    Task<Dictionary<string, Artist>> GetArtistsByKeysAsync(IEnumerable<string> spotifyArtistIds, IEnumerable<string> localArtistNames, CancellationToken cancellationToken = default);
    Task<Dictionary<string, PlaylistItem>> GetPlaylistItemsByPlaylistIdAsync(Guid playlistId, CancellationToken cancellationToken = default);
    Task<List<PlaylistItem>> GetPlaylistItemsByPlaylistIdsAsync(IEnumerable<Guid> playlistIds, CancellationToken cancellationToken = default);

    Task AddPlaylistsAsync(IEnumerable<Playlist> playlists, CancellationToken cancellationToken = default);
    Task AddItemsAsync(IEnumerable<Item> items, CancellationToken cancellationToken = default);
    Task AddArtistsAsync(IEnumerable<Artist> artists, CancellationToken cancellationToken = default);
    Task AddPlaylistItemsAsync(IEnumerable<PlaylistItem> playlistItems, CancellationToken cancellationToken = default);

    Task RemovePlaylistsAsync(IEnumerable<Playlist> playlists, CancellationToken cancellationToken = default);
    Task RemovePlaylistItemsAsync(IEnumerable<PlaylistItem> playlistItems, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class LibraryRepository(AmplystDbContext dbContext) : ILibraryRepository
{
    public async Task<List<Playlist>> GetPlaylistsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Playlists
            .Where(p => p.CreatedBy == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, Item>> GetItemsByUrisAsync(IEnumerable<string> spotifyItemUris, CancellationToken cancellationToken = default)
    {
        var uris = spotifyItemUris.Distinct().ToList();
        if (uris.Count == 0) return [];

        return await dbContext.Items
            .Include(i => i.Artists)
            .Where(i => uris.Contains(i.SpotifyItemUri))
            .ToDictionaryAsync(i => i.SpotifyItemUri, cancellationToken);
    }

    public async Task<Dictionary<string, Artist>> GetArtistsByKeysAsync(IEnumerable<string> spotifyArtistIds, IEnumerable<string> localArtistNames, CancellationToken cancellationToken = default)
    {
        var ids = spotifyArtistIds.Distinct().ToList();
        var names = localArtistNames.Distinct().ToList();
        if (ids.Count == 0 && names.Count == 0) return [];

        var artists = await dbContext.Artists
            .Where(a => (a.SpotifyArtistId != null && ids.Contains(a.SpotifyArtistId))
                     || (a.SpotifyArtistId == null && a.SpotifyArtistUri == null && names.Contains(a.Name)))
            .ToListAsync(cancellationToken);

        return artists.ToDictionary(a => a.GetMatchKey());
    }

    public async Task<Dictionary<string, PlaylistItem>> GetPlaylistItemsByPlaylistIdAsync(Guid playlistId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PlaylistItems
            .Where(pi => pi.PlaylistId == playlistId && pi.SpotifyItemUri != null)
            .ToDictionaryAsync(pi => pi.SpotifyItemUri!, cancellationToken);
    }

    public async Task<List<PlaylistItem>> GetPlaylistItemsByPlaylistIdsAsync(IEnumerable<Guid> playlistIds, CancellationToken cancellationToken = default)
    {
        var ids = playlistIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return await dbContext.PlaylistItems
            .Where(pi => ids.Contains(pi.PlaylistId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddPlaylistsAsync(IEnumerable<Playlist> playlists, CancellationToken cancellationToken = default)
    {
        await dbContext.Playlists.AddRangeAsync(playlists, cancellationToken);
    }

    public async Task AddItemsAsync(IEnumerable<Item> items, CancellationToken cancellationToken = default)
    {
        await dbContext.Items.AddRangeAsync(items, cancellationToken);
    }

    public async Task AddArtistsAsync(IEnumerable<Artist> artists, CancellationToken cancellationToken = default)
    {
        await dbContext.Artists.AddRangeAsync(artists, cancellationToken);
    }

    public async Task AddPlaylistItemsAsync(IEnumerable<PlaylistItem> playlistItems, CancellationToken cancellationToken = default)
    {
        await dbContext.PlaylistItems.AddRangeAsync(playlistItems, cancellationToken);
    }

    public Task RemovePlaylistsAsync(IEnumerable<Playlist> playlists, CancellationToken cancellationToken = default)
    {
        dbContext.Playlists.RemoveRange(playlists);
        return Task.CompletedTask;
    }

    public Task RemovePlaylistItemsAsync(IEnumerable<PlaylistItem> playlistItems, CancellationToken cancellationToken = default)
    {
        dbContext.PlaylistItems.RemoveRange(playlistItems);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}