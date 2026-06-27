namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a user's request to be notified when tickets become available for an event.
/// Tracks notification status, expiry, and the quantity of tickets desired.
/// </summary>
/// <remarks>
/// The waiting list allows users to express interest in tickets for sold-out events. Users can be
/// automatically notified when tickets become available, and entries expire after a configurable period
/// to prevent stale waiting list entries.
/// </remarks>
public class WaitingList : IAuditable
{
    /// <summary>
    /// Gets or sets the unique identifier for the waiting list entry.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the event ID (foreign key).
    /// </summary>
    /// <value>The event identifier.</value>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the ticket type ID (foreign key).
    /// </summary>
    /// <value>The ticket type identifier.</value>
    public int TicketTypeId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the waiting list entry (foreign key).
    /// </summary>
    /// <value>The user identifier.</value>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of tickets the user wants to purchase.
    /// </summary>
    /// <value>The number of tickets desired.</value>
    public int QuantityWanted { get; set; }

    /// <summary>
    /// Gets or sets the current status of the waiting list entry.
    /// </summary>
    /// <value>The status (Waiting, Notified, Converted, or Expired).</value>
    public WaitingListStatus Status { get; set; } = WaitingListStatus.Waiting;

    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was notified of ticket availability.
    /// </summary>
    /// <value>The notification timestamp, or null if not yet notified.</value>
    public DateTime? NotifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration date and time for this waiting list entry.
    /// </summary>
    /// <value>The expiry timestamp, or null if no expiry.</value>
    public DateTime? ExpiresAt { get; set; }

    // Navigation
    public Event Event { get; set; } = null!;
    public TicketType TicketType { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}