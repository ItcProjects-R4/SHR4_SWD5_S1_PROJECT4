namespace EventifyPro.BLL.DTOs.Booking;

/// <summary>
/// Data transfer object for creating a new booking.
/// </summary>
public record BookingCreateDto
{
    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the list of booking items (ticket types and quantities).
    /// </summary>
    [Required, MinLength(1)]
    public List<BookingItemRequestDto> Items { get; init; } = [];

    /// <summary>
    /// Gets or sets the optional waiting list entry identifier.
    /// </summary>
    public int? WaitingListId { get; init; }
}
