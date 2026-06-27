namespace Eventify.Domain.Entities;

public class SavedEvent : AuditableEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int EventId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Event Event { get; set; } = null!;
}
