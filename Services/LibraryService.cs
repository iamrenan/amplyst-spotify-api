using System.ComponentModel.DataAnnotations;
using amplyst_spotify_api.DTOs;
using amplyst_spotify_api.Exceptions;
using amplyst_spotify_api.Models.Core;
using amplyst_spotify_api.Repositories;

namespace amplyst_spotify_api.Services;

public interface ILibraryService
{
    Task<SyncResponseDTO> CreateSyncDataAsync(string userId);
    Task<SyncResponseDTO?> GetSyncDataAsync(Guid syncRunId);
}

public class LibraryService(ILibraryRepository syncRepository) : ILibraryService
{
    public async Task<SyncResponseDTO> CreateSyncDataAsync(string userId)
    {
        if (syncRepository.HasPendingSync(userId))
        {
            throw new PendingSyncException("A sync request is already pending for this user.");
        }

        var sync = new SyncData
        {
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            SyncRunId = Guid.NewGuid(),
            Status = SyncStatus.Queued
        };

        await syncRepository.AddSyncDataAsync(sync);
        return new SyncResponseDTO(sync.Status, sync.SyncRunId);
    }

    public async Task<SyncResponseDTO?> GetSyncDataAsync(Guid syncRunId)
    {
        var sync = await syncRepository.GetSyncDataByIdAsync(syncRunId);
        if (sync == null)
        {
            return null;
        }
        return new SyncResponseDTO(sync.Status, sync.SyncRunId);
    }
}