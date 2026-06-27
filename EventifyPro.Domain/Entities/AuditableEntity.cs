namespace Eventify.Domain.Entities;

/// <summary>
/// Base abstract class for auditable entities.
/// Unifies CreatedAt and UpdatedAt properties under a single base type.
/// </summary>
public abstract class AuditableEntity : IAuditable
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
