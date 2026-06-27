namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a category for organizing events (e.g., Sports, Music, Technology).
/// Categories help users filter and discover events of interest.
/// </summary>
/// <remarks>
/// Categories provide a taxonomy for organizing events, enabling users to browse and filter
/// events by type. Each event is associated with one category, and categories can be managed
/// by administrators.
/// </remarks>
public class Category : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the category.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the category.
    /// </summary>
    /// <value>The category name.</value>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation
    public ICollection<Event> Events { get; set; } = [];
}