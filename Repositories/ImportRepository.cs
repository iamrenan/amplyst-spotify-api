using amplyst_spotify_api.Data;
using amplyst_spotify_api.Entities;
using amplyst_spotify_api.Models.Core;
using amplyst_spotify_api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace amplyst_spotify_api.Repositories;

public interface IImportRepository
{
    Task<ImportJob?> GetImportJobByIdAsync(Guid Id, CancellationToken cancellationToken = default);
    Task CreateImportJobAsync(ImportJob request, CancellationToken cancellationToken = default);
    Task UpdateImportJobAsync(ImportJob request, CancellationToken cancellationToken = default);
    Task<bool> UserHasPendingImportAsync(string userId, CancellationToken cancellationToken = default);
}

public partial class ImportRepository(AmplystDbContext dbContext, ILogger<ImportRepository> logger) : IImportRepository
{
    public async Task<ImportJob?> GetImportJobByIdAsync(Guid Id, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.ImportJobs.FindAsync([Id], cancellationToken);
        if (job is null)
        {
            LogImportJobNotFound(Id);
        }
        return job;
    }

    public async Task<List<Guid>> GetAllSyncIdsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ImportJobs.Select(s => s.Id).ToListAsync(cancellationToken);
    }

    public async Task CreateImportJobAsync(ImportJob request, CancellationToken cancellationToken = default)
    {
        await dbContext.ImportJobs.AddAsync(request, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateImportJobAsync(ImportJob request, CancellationToken cancellationToken = default)
    {
        dbContext.ImportJobs.Update(request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UserHasPendingImportAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ImportJobs.AnyAsync(s => s.CreatedBy == userId && (s.Status == ImportJobStatus.Queued || s.Status == ImportJobStatus.InProgress), cancellationToken);
    }

    public async Task<List<Item>> GetAllUserTracksAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Items.AsNoTracking().Include(t => t.Artists).ToListAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Import job with ID {Id} not found.")]
    private partial void LogImportJobNotFound(Guid id);
}