namespace Eventify.Domain.Entities;

/// <summary>
/// Join table entity representing the assignment of a Scanner user to a specific Event.
/// </summary>
public class EventScanner : AuditableEntity
{
    public int Id { get; set; }
    
    public string ScannerId { get; set; } = string.Empty;
    public int EventId { get; set; }
    
    // Navigation
    public ApplicationUser Scanner { get; set; } = null!;
    public Event Event { get; set; } = null!;
}
