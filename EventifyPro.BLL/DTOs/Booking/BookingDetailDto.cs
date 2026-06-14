namespace EventifyPro.BLL.DTOs.Booking;

/// <summary>
/// Data transfer object representing a booking item in a response.
/// </summary>
public record BookingItemResponseDto
{
    /// <summary>
    /// Gets or sets the booking item identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the ticket type identifier.
    /// </summary>
    public int TicketTypeId { get; init; }

    /// <summary>
    /// Gets or sets the name of the ticket type.
    /// </summary>
    public string TicketTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of tickets ordered.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Gets or sets the unit price of each ticket.
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Gets the subtotal (quantity × unit price).
    /// </summary>
    public decimal Subtotal => Quantity * UnitPrice;
}

/// <summary>
/// Data transfer object containing detailed booking information.
/// </summary>
public record BookingDetailDto
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
    /// Gets or sets the platform/service fee charged for this booking.
    /// </summary>
    public decimal ServiceFee { get; init; }

    /// <summary>
    /// Gets or sets the status of the booking (e.g., Confirmed, Cancelled, Pending).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique booking reference number.
    /// </summary>
    public string BookingReference { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for cancellation if applicable.
    /// </summary>
    public string? CancellationReason { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the booking was created.
    /// </summary>
    public DateTime BookingDate { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the booking was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets or sets the list of booking items included in this booking.
    /// </summary>
    public List<BookingItemResponseDto> Items { get; init; } = [];
}
