namespace EventifyPro.BLL.DTOs.WaitingList;

/// <summary>
/// Data transfer object containing waiting list entry information.
/// </summary>
public record WaitingListResponseDto
{
    /// <summary>
    /// Gets or sets the waiting list entry identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the title of the event.
    /// </summary>
    public string EventTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the ticket type identifier.
    /// </summary>
    public int TicketTypeId { get; init; }

    /// <summary>
    /// Gets or sets the name of the ticket type.
    /// </summary>
    public string TicketTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier on the waiting list.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the desired quantity of tickets.
    /// </summary>
    public int QuantityWanted { get; init; }

    /// <summary>
    /// Gets or sets the status of the waiting list entry (e.g., Pending, Notified, Purchased, Expired).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the entry was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
