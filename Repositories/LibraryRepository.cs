using amplyst_spotify_api.Data;
using amplyst_spotify_api.Models.Core;

namespace amplyst_spotify_api.Repositories;

public interface ILibraryRepository
{
    public Task<SyncData?> GetSyncDataByIdAsync(Guid syncRunId, CancellationToken cancellationToken = default);
    public Task AddSyncDataAsync(SyncData syncData, CancellationToken cancellationToken = default);
    public Task UpdateSyncDataAsync(SyncData syncData, CancellationToken cancellationToken = default);
    public bool HasPendingSync(string userId);
}

public class LibraryRepository(AmplystDbContext dbContext) : ILibraryRepository
{
    public async Task<SyncData?> GetSyncDataByIdAsync(Guid syncRunId, CancellationToken cancellationToken)
    {
        return await dbContext.Syncs.FindAsync([syncRunId], cancellationToken);
    }

    public async Task AddSyncDataAsync(SyncData syncData, CancellationToken cancellationToken)
    {
        await dbContext.Syncs.AddAsync(syncData, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSyncDataAsync(SyncData syncData, CancellationToken cancellationToken)
    {
        dbContext.Syncs.Update(syncData);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public bool HasPendingSync(string userId)
    {
        return dbContext.Syncs.Any(s => s.CreatedBy == userId && s.Status == SyncStatus.Queued);
    }
}