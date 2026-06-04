namespace EventifyPro.BLL.DTOs.TicketType;

/// <summary>
/// Data transfer object containing ticket type information.
/// </summary>
public record TicketTypeResponseDto
{
    /// <summary>
    /// Gets or sets the ticket type identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the event identifier this ticket type belongs to.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the ticket type name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the price of the ticket.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Gets or sets the total quantity of tickets available.
    /// </summary>
    public int TotalQuantity { get; init; }

    /// <summary>
    /// Gets or sets the quantity of tickets sold.
    /// </summary>
    public int SoldQuantity { get; init; }

    /// <summary>
    /// Gets the remaining quantity of available tickets.
    /// </summary>
    public int AvailableQuantity => TotalQuantity - SoldQuantity;

    /// <summary>
    /// Gets or sets the description of the ticket type.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the date when ticket sales start.
    /// </summary>
    public DateTime? SaleStartDate { get; init; }

    /// <summary>
    /// Gets or sets the date when ticket sales end.
    /// </summary>
    public DateTime? SaleEndDate { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the ticket type was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the ticket type was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the ticket type is available for purchase.
    /// </summary>
    public bool IsAvailable => AvailableQuantity > 0;
}
