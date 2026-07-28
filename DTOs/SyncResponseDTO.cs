using amplyst_spotify_api.Models.Core;

namespace amplyst_spotify_api.DTOs;

public record SyncResponseDTO(SyncStatus Status, Guid SyncRunId);