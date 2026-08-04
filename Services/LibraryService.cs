using amplyst_spotify_api.Entities;
using amplyst_spotify_api.Mapping;
using amplyst_spotify_api.Models.Spotify;
using amplyst_spotify_api.Repositories;

namespace amplyst_spotify_api.Services;

public interface ILibraryService
{
    Task<int> UpdateUserLibraryAsync(List<SimplifiedPlaylist> playlists, string accessToken, string userId, CancellationToken cancellationToken = default);
}

public partial class LibraryService(ILibraryRepository repository, ISpotifyClientService spotifyClientService, ILogger<LibraryService> logger) : ILibraryService
{
    public async Task<int> UpdateUserLibraryAsync(List<SimplifiedPlaylist> playlists, string accessToken, string userId, CancellationToken cancellationToken = default)
    {
        var changedPlaylists = await UpsertPlaylistsAsync(playlists, userId, cancellationToken);

        foreach (var playlist in changedPlaylists)
        {
            await SyncPlaylistItemsAsync(playlist, accessToken, userId, cancellationToken);
        }

        return changedPlaylists.Count;
    }

    private async Task<List<Playlist>> UpsertPlaylistsAsync(List<SimplifiedPlaylist> playlists, string userId, CancellationToken cancellationToken)
    {
        var existingPlaylists = await repository.GetPlaylistsByUserIdAsync(userId, cancellationToken);
        var existingBySpotifyId = existingPlaylists
            .Where(p => p.SpotifyPlaylistId is not null)
            .ToDictionary(p => p.SpotifyPlaylistId!);

        var playlistsToAdd = new List<Playlist>();
        var changedPlaylists = new List<Playlist>();

        foreach (var playlist in playlists)
        {
            if (existingBySpotifyId.TryGetValue(playlist.Id, out var existing))
            {
                if (existing.SpotifySnapshotId == playlist.SnapshotId)
                {
                    LogSkippingPlaylist(playlist.Id);
                    continue;
                }

                existing.Name = playlist.Name;
                existing.SpotifySnapshotId = playlist.SnapshotId;
                existing.UpdatedBy = userId;
                changedPlaylists.Add(existing);
                LogPlaylistUpdated(playlist.Id);
            }
            else
            {
                var entity = playlist.ToEntity();
                entity.CreatedBy = userId;
                playlistsToAdd.Add(entity);
                changedPlaylists.Add(entity);
                LogPlaylistAdded(playlist.Id);
            }
        }

        if (playlistsToAdd.Count > 0)
        {
            await repository.AddPlaylistsAsync(playlistsToAdd, cancellationToken);
        }

        await RemoveMissingPlaylistsAsync(existingPlaylists, playlists, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        return changedPlaylists;
    }

    private async Task RemoveMissingPlaylistsAsync(List<Playlist> existingPlaylists, List<SimplifiedPlaylist> fetchedPlaylists, CancellationToken cancellationToken)
    {
        var fetchedIds = fetchedPlaylists.Select(p => p.Id).ToHashSet();
        var removedPlaylists = existingPlaylists
            .Where(p => p.SpotifyPlaylistId is not null && !fetchedIds.Contains(p.SpotifyPlaylistId))
            .ToList();

        if (removedPlaylists.Count == 0)
        {
            return;
        }

        var orphanedItems = await repository.GetPlaylistItemsByPlaylistIdsAsync(removedPlaylists.Select(p => p.Id), cancellationToken);
        if (orphanedItems.Count > 0)
        {
            await repository.RemovePlaylistItemsAsync(orphanedItems, cancellationToken);
        }

        await repository.RemovePlaylistsAsync(removedPlaylists, cancellationToken);

        foreach (var removed in removedPlaylists)
        {
            LogPlaylistRemoved(removed.SpotifyPlaylistId!);
        }
    }

    private async Task SyncPlaylistItemsAsync(Playlist playlist, string accessToken, string userId, CancellationToken cancellationToken)
    {
        if (playlist.SpotifyPlaylistId is null)
        {
            return;
        }

        var playlistItems = (await spotifyClientService.FetchAllPlaylistItemsAsync(accessToken, playlist.SpotifyPlaylistId, cancellationToken)).ToList();

        Dictionary<string, Item> itemsByUri = playlistItems.Count > 0
            ? await UpsertItemsAsync(playlistItems, userId, cancellationToken)
            : [];
        var existingLinks = await repository.GetPlaylistItemsByPlaylistIdAsync(playlist.Id, cancellationToken);
        var fetchedItemUris = playlistItems.Where(i => i.Item != null).Select(i => i.Item!.Uri).ToHashSet();

        var linksToAdd = new List<Entities.PlaylistItem>();
        foreach (var item in playlistItems)
        {
            if (item.Item is null)
            {
                LogNullItem(item.AddedAt, item.AddedBy?.Id ?? "unknown");
                continue;
            }

            if (!itemsByUri.TryGetValue(item.Item.Uri, out var playlistItem))
            {
                continue;
            }

            if (existingLinks.TryGetValue(item.Item.Uri, out var existingLink))
            {
                existingLink.SpotifyAddedAt = item.AddedAt;
                existingLink.SpotifyAddedById = item.AddedBy?.Id;
                existingLink.UpdatedBy = userId;
                continue;
            }

            linksToAdd.Add(item.ToPlaylistItem(playlist, playlistItem, userId));
            LogPlaylistItemAdded(playlist.SpotifyPlaylistId, item.Item.Uri);
        }

        var linksToRemove = existingLinks
            .Where(kvp => !fetchedItemUris.Contains(kvp.Key))
            .Select(kvp => kvp.Value)
            .ToList();

        if (linksToAdd.Count > 0)
        {
            await repository.AddPlaylistItemsAsync(linksToAdd, cancellationToken);
        }
        if (linksToRemove.Count > 0)
        {
            await repository.RemovePlaylistItemsAsync(linksToRemove, cancellationToken);
            foreach (var link in linksToRemove)
            {
                LogPlaylistItemRemoved(playlist.SpotifyPlaylistId, link.SpotifyItemUri!);
            }
        }
        await repository.SaveChangesAsync(cancellationToken);

        LogPlaylistItemsSynced(playlist.SpotifyPlaylistId, playlistItems.Count);
    }

    private async Task<Dictionary<string, Item>> UpsertItemsAsync(List<Models.Spotify.PlaylistItem> playlistItems, string userId, CancellationToken cancellationToken)
    {
        var uniqueItemsByUri = new Dictionary<string, Item>();
        foreach (var item in playlistItems)
        {
            if (item.Item is null)
            {
                LogNullItem(item.AddedAt, item.AddedBy?.Id ?? "unknown");
                continue;
            }

            try
            {
                uniqueItemsByUri.TryAdd(item.Item.Uri, item.Item.ToItem(item.IsLocal));
            }
            catch (Exception ex)
            {
                LogItemMappingFailed(ex, item.Item.Uri, item.Item.Type);
            }
        }

        var artistsByKey = new Dictionary<string, Artist>();
        foreach (var item in uniqueItemsByUri.Values)
        {
            foreach (var artist in item.Artists)
            {
                artistsByKey.TryAdd(artist.GetMatchKey(), artist);
            }
        }

        var resolvedArtists = await UpsertArtistsAsync(artistsByKey.Values, userId, cancellationToken);
        var existingItems = await repository.GetItemsByUrisAsync(uniqueItemsByUri.Keys, cancellationToken);

        var itemsToAdd = new List<Item>();
        var result = new Dictionary<string, Item>();

        foreach (var (spotifyItemUri, mapped) in uniqueItemsByUri)
        {
            var resolvedItemArtists = mapped.Artists
                .Select(a => resolvedArtists[a.GetMatchKey()])
                .ToList();

            if (existingItems.TryGetValue(spotifyItemUri, out var existing))
            {
                existing.Name = mapped.Name;
                existing.Artists = resolvedItemArtists;
                existing.UpdatedBy = userId;
                result[spotifyItemUri] = existing;
            }
            else
            {
                mapped.Artists = resolvedItemArtists;
                mapped.CreatedBy = userId;
                itemsToAdd.Add(mapped);
                result[spotifyItemUri] = mapped;
                LogItemAdded(spotifyItemUri, mapped.Name);
            }
        }

        if (itemsToAdd.Count > 0)
        {
            await repository.AddItemsAsync(itemsToAdd, cancellationToken);
        }

        return result;
    }

    private async Task<Dictionary<string, Artist>> UpsertArtistsAsync(IEnumerable<Artist> artists, string userId, CancellationToken cancellationToken)
    {
        var artistList = artists.ToList();
        var spotifyIds = artistList.Where(a => a.SpotifyArtistId is not null).Select(a => a.SpotifyArtistId!);
        var localArtistNames = artistList.Where(a => a.SpotifyArtistId is null && a.SpotifyArtistUri is null).Select(a => a.Name);
        var existingArtists = await repository.GetArtistsByKeysAsync(spotifyIds, localArtistNames, cancellationToken);

        var artistsToAdd = new List<Artist>();
        var result = new Dictionary<string, Artist>();

        foreach (var artist in artistList)
        {
            var key = artist.GetMatchKey();
            if (existingArtists.TryGetValue(key, out var existing))
            {
                existing.Name = artist.Name;
                existing.UpdatedBy = userId;
                result[key] = existing;
            }
            else
            {
                artist.CreatedBy = userId;
                artistsToAdd.Add(artist);
                result[key] = artist;
                LogArtistAdded(key, artist.Name);
            }
        }

        if (artistsToAdd.Count > 0)
        {
            await repository.AddArtistsAsync(artistsToAdd, cancellationToken);
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Playlist {SpotifyPlaylistId} skipped (No newer snapshot)")]
    private partial void LogSkippingPlaylist(string spotifyPlaylistId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Synced {TrackCount} tracks for playlist {SpotifyPlaylistId}")]
    private partial void LogPlaylistItemsSynced(string spotifyPlaylistId, int trackCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Playlist {SpotifyPlaylistId} added")]
    private partial void LogPlaylistAdded(string spotifyPlaylistId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Playlist {SpotifyPlaylistId} updated")]
    private partial void LogPlaylistUpdated(string spotifyPlaylistId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Playlist {SpotifyPlaylistId} removed")]
    private partial void LogPlaylistRemoved(string spotifyPlaylistId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Playlist item {SpotifyItemId} added to playlist {SpotifyPlaylistId}")]
    private partial void LogPlaylistItemAdded(string spotifyPlaylistId, string spotifyItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Playlist item {SpotifyItemId} removed from playlist {SpotifyPlaylistId}")]
    private partial void LogPlaylistItemRemoved(string spotifyPlaylistId, string spotifyItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Item {SpotifyItemId} ({ItemName}) added")]
    private partial void LogItemAdded(string spotifyItemId, string itemName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Artist {SpotifyArtistId} ({ArtistName}) added")]
    private partial void LogArtistAdded(string spotifyArtistId, string artistName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Playlist item skipped (Item is null) - Added at {AddedAt} by {AddedBy}")]
    private partial void LogNullItem(DateTime? addedAt, string addedBy);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to map playlist item {SpotifyItemUri} (type: {ItemType}) to entity; item skipped")]
    private partial void LogItemMappingFailed(Exception exception, string spotifyItemUri, string itemType);
}
