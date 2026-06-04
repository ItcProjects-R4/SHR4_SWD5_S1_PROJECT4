namespace EventifyPro.BLL.DTOs.Booking;

/// <summary>
/// Data transfer object containing a summary of booking information.
/// </summary>
public record BookingSummaryDto
{
    /// <summary>
    /// Gets or sets the booking identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the user identifier of the person who made the booking.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the title of the event.
    /// </summary>
    public string EventTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount for the booking.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets or sets the status of the booking.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique booking reference number.
    /// </summary>
    public string BookingReference { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the booking was created.
    /// </summary>
    public DateTime BookingDate { get; init; }
}
