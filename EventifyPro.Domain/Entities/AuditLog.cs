namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a record of a change made to a database table for auditing.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "Insert", "Update", "Delete"
    public string EntityId { get; set; } = string.Empty; // Primary key value
    public string? OldValues { get; set; } // JSON of old values
    public string? NewValues { get; set; } // JSON of new values
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
