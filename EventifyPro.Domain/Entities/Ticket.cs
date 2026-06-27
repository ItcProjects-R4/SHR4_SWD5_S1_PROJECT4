namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a single ticket issued for a booking.
/// Contains QR code for validation, usage tracking, and scan log history.
/// </summary>
/// <remarks>
/// Each ticket corresponds to one attendee slot for an event. Tickets are generated from bookings
/// and assigned unique QR codes for validation at event entry. The ticket tracks whether it has been
/// used and by whom, supporting attendance verification and re-entry prevention.
/// </remarks>
public class Ticket : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the ticket.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the event ID (denormalized for query efficiency).
    /// </summary>
    /// <value>The event identifier.</value>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the booking ID (foreign key).
    /// </summary>
    /// <value>The parent booking identifier.</value>
    public int BookingId { get; set; }

    /// <summary>
    /// Gets or sets the ticket type ID (foreign key).
    /// </summary>
    /// <value>The ticket type identifier.</value>
    public int TicketTypeId { get; set; }

    /// <summary>
    /// Gets or sets the QR code string for ticket validation.
    /// </summary>
    /// <value>The unique QR code.</value>
    public string QRCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the ticket has been used for entry.
    /// </summary>
    /// <value><c>true</c> if the ticket has been scanned at event entry; otherwise, <c>false</c>.</value>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Gets or sets the date and time when the ticket was used.
    /// </summary>
    /// <value>The scan timestamp, or null if the ticket has not been used.</value>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the scanner who validated this ticket.
    /// </summary>
    /// <value>The scanner's user ID, or null if not yet scanned.</value>
    public string? ScannedById { get; set; }

    // Navigation
    public Event Event { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
    public TicketType TicketType { get; set; } = null!;
    public ApplicationUser? Scanner { get; set; }
    public ICollection<ScanLog> ScanLogs { get; set; } = [];
}