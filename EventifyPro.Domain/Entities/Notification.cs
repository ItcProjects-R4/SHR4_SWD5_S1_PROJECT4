namespace Eventify.Domain.Entities;

public class Notification : AuditableEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public string? RedirectUrl { get; set; }

    // Navigation Property
    public ApplicationUser User { get; set; } = null!;
}
