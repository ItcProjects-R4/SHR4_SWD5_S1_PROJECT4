namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a booking made by a user for an event.
/// Contains booking items (tickets), payment information, and refund history.
/// </summary>
/// <remarks>
/// A booking serves as the primary transaction record when a user purchases tickets for an event.
/// It tracks the overall booking status, total amount, and maintains relationships to individual
/// ticket items, payment records, and any refunds initiated against the booking.
/// </remarks>
public class Booking : IAuditable
{
    /// <summary>
    /// Gets or sets the unique identifier for the booking.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the person who made the booking (foreign key).
    /// </summary>
    /// <value>The booking user's ID.</value>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event ID for which the booking was made (foreign key).
    /// </summary>
    /// <value>The event identifier.</value>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the total amount for the booking.
    /// </summary>
    /// <value>The sum of all booking items in the booking.</value>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the platform/service fee charged for this booking.
    /// </summary>
    public decimal ServiceFee { get; set; }

    /// <summary>
    /// Gets or sets the current status of the booking.
    /// </summary>
    /// <value>The booking status (Pending, Confirmed, Cancelled, or Refunded).</value>
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>
    /// Gets or sets the unique booking reference number.
    /// </summary>
    /// <value>A unique identifier for external reference.</value>
    public string BookingReference { get; set; } = string.Empty;

    public string? CancellationReason { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public ICollection<BookingItem> Items { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
    public Payment? Payment { get; set; }
    public ICollection<Refund> Refunds { get; set; } = [];
}