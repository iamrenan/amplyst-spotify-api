using amplyst_spotify_api.Models.Enums;

namespace amplyst_spotify_api.Models.Core;

public record ImportJobResponseDTO(Guid Id, ImportJobStatus Status, string? ErrorMessage);