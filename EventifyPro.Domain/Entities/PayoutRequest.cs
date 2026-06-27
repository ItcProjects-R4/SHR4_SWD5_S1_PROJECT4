namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a payout request made by an Organizer to withdraw earnings.
/// Tracks transaction status, amount, bank connection, and reference audit logs.
/// </summary>
public class PayoutRequest : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// The user ID of the Organizer requesting the payout.
    /// </summary>
    public string OrganizerId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The current status of the request (e.g. "Pending", "Completed", "Rejected").
    /// </summary>
    public string Status { get; set; } = "Pending";

    public string Method { get; set; } = "Bank Transfer";

    public DateTime? ProcessedAt { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public ApplicationUser Organizer { get; set; } = null!;
}
