namespace EventifyPro.BLL.DTOs.WaitingList;

/// <summary>
/// Data transfer object for joining a waiting list.
/// </summary>
public record WaitingListJoinDto
{
    /// <summary>
    /// Gets or sets the event identifier to join the waiting list for.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the ticket type identifier.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int TicketTypeId { get; init; }

    /// <summary>
    /// Gets or sets the desired quantity of tickets.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int QuantityWanted { get; init; }
}
