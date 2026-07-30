using amplyst_spotify_api.Common;
using amplyst_spotify_api.Models.Enums;

namespace amplyst_spotify_api.Entities;

public class ImportJob : Auditable
{
    public required ImportJobStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
