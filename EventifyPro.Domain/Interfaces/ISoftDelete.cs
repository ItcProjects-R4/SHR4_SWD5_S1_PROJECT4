namespace Eventify.Domain.Interfaces;

/// <summary>
/// Interface to support soft deleting entities.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}
