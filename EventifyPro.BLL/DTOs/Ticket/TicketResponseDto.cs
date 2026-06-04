namespace EventifyPro.BLL.DTOs.Ticket;

/// <summary>
/// Data transfer object containing ticket information.
/// </summary>
public record TicketResponseDto
{
    /// <summary>
    /// Gets or sets the ticket identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the event identifier for this ticket.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the booking identifier this ticket belongs to.
    /// </summary>
    public int BookingId { get; init; }

    /// <summary>
    /// Gets or sets the ticket type identifier.
    /// </summary>
    public int TicketTypeId { get; init; }

    /// <summary>
    /// Gets or sets the name of the ticket type.
    /// </summary>
    public string TicketTypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the QR code for this ticket.
    /// </summary>
    public string QRCode { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the ticket has been used/scanned.
    /// </summary>
    public bool IsUsed { get; init; }

    /// <summary>
    /// Gets or sets the name of the person who used the ticket.
    /// </summary>
    public string? UsedByName { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the ticket was used/scanned.
    /// </summary>
    public DateTime? UsedAt { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the ticket was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
