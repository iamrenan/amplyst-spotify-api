namespace amplyst_spotify_api.Models.Core;

public class SyncData : Auditable
{
    public required Guid SyncRunId { get; init; }
    public required SyncStatus Status { get; init; }
}

public enum SyncStatus
{
    Queued,
    InProgress,
    Completed,
    Failed
}