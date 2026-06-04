namespace EventifyPro.BLL.DTOs.Booking;

/// <summary>
/// Data transfer object for requesting booking items (ticket types and quantities).
/// </summary>
public record BookingItemRequestDto
{
    /// <summary>
    /// Gets or sets the ticket type identifier.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int TicketTypeId { get; init; }

    /// <summary>
    /// Gets or sets the quantity of tickets to book.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}
