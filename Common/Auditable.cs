namespace amplyst_spotify_api.Common;

public abstract class Auditable : Entity
{
    /// <summary>
    /// The date and time when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the entity was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The user who created the entity (optional).
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// The user who last updated the entity (optional).
    /// </summary>
    public string? UpdatedBy { get; set; }
}
