using amplyst_spotify_api.Entities;
using amplyst_spotify_api.Exceptions;
using amplyst_spotify_api.Mapping;
using amplyst_spotify_api.Models.Core;
using amplyst_spotify_api.Models.Enums;
using amplyst_spotify_api.Models.Spotify;
using amplyst_spotify_api.Repositories;

namespace amplyst_spotify_api.Services;

public interface IImportService
{
    Task<ImportJobResponseDTO> CreateImportJobAsync(string userId, CancellationToken cancellationToken = default);
    Task<ImportJobResponseDTO?> GetImportJobByIdAsync(Guid importJobId, CancellationToken cancellationToken = default);
}

public partial class ImportService(IServiceScopeFactory serviceScopeFactory, ILogger<ImportService> logger) : IImportService
{
    public async Task<ImportJobResponseDTO> CreateImportJobAsync(string userId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IImportRepository repository = scope.ServiceProvider.GetRequiredService<IImportRepository>();

        if (await repository.UserHasPendingImportAsync(userId, cancellationToken))
        {
            throw new ImportAlreadyInProgressException("A sync request is already pending for this user.");
        }

        ImportJob job = new()
        {
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Id = Guid.NewGuid(),
            Status = ImportJobStatus.Queued
        };

        await repository.CreateImportJobAsync(job, cancellationToken);
        _ = StartSyncJobAsync(job.Id, userId, cancellationToken);
        return new ImportJobResponseDTO(job.Id, job.Status, job.ErrorMessage);
    }

    public async Task<ImportJobResponseDTO?> GetImportJobByIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IImportRepository>();

        var job = await repository.GetImportJobByIdAsync(importJobId, cancellationToken);
        if (job == null)
        {
            return null;
        }
        return new ImportJobResponseDTO(job.Id, job.Status, job.ErrorMessage);
    }

    private static async Task UpdateImportJobStatusAsync(IImportRepository repository, ImportJob job, ImportJobStatus status, CancellationToken cancellationToken = default)
    {
        job.UpdatedAt = DateTime.UtcNow;
        job.Status = status;
        await repository.UpdateImportJobAsync(job, cancellationToken);
    }

    private async Task StartSyncJobAsync(Guid jobId, string userId, CancellationToken cancellationToken = default)
    {
        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["JobId"] = jobId,
            ["UserId"] = userId
        });

        using var asyncScope = serviceScopeFactory.CreateScope();
        var importRepository = asyncScope.ServiceProvider.GetRequiredService<IImportRepository>();
        var spotifyClientService = asyncScope.ServiceProvider.GetRequiredService<ISpotifyClientService>();
        var tokenService = asyncScope.ServiceProvider.GetRequiredService<ITokenService>();
        var libraryService = asyncScope.ServiceProvider.GetRequiredService<ILibraryService>();

        try
        {
            var job = await importRepository.GetImportJobByIdAsync(jobId, cancellationToken) ?? throw new InvalidOperationException($"Import job {jobId} was not found.");
            await UpdateImportJobStatusAsync(importRepository, job, ImportJobStatus.InProgress, cancellationToken);

            var token = await tokenService.GetTokenAsync(userId, cancellationToken);
            int playlistCount = 0;
            int trackCount = 0;
            List<SimplifiedPlaylist> playlists = [];

            await foreach (var playlist in spotifyClientService.FetchAllCurrentUserPlaylistsAsync(token!.AccessToken, cancellationToken))
            {
                playlists.Add(playlist);
                playlistCount++;
                trackCount += playlist.Items.Total;
            }

            var addedPlaylistsCount = await libraryService.UpdateUserLibraryAsync(playlists, token.AccessToken, userId, cancellationToken);

            await UpdateImportJobStatusAsync(importRepository, job, ImportJobStatus.Completed, cancellationToken);

            LogSuccess(playlistCount, trackCount, addedPlaylistsCount);
        }
        catch (OperationCanceledException)
        {
            LogCancelledSync();
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job failed: {ExceptionMessage}", ex.Message);
            try
            {
                var job = await importRepository.GetImportJobByIdAsync(jobId, CancellationToken.None);
                if (job is not null)
                {
                    job.ErrorMessage = ex.Message;
                    await UpdateImportJobStatusAsync(importRepository, job, ImportJobStatus.Failed, CancellationToken.None);
                }
            }
            catch (Exception updateEx)
            {
                logger.LogError(updateEx, "Failed to update import job status to Failed.");
            }
        }
        finally
        {
            logScope?.Dispose();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Import job completed successfully. Playlists: {PlaylistCount}, Tracks: {TrackCount}, Added Playlists: {AddedPlaylistsCount}")]
    private partial void LogSuccess(int playlistCount, int trackCount, int addedPlaylistsCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Import job cancelled.")]
    private partial void LogCancelledSync();
}