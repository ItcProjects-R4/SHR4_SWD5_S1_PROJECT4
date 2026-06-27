namespace EventifyPro.BLL.DTOs.TicketType;

/// <summary>
/// Data transfer object for creating a new ticket type.
/// </summary>
public record TicketTypeCreateDto
{
    /// <summary>
    /// Gets or sets the event identifier this ticket type belongs to.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the ticket type name.
    /// </summary>
    [Required, StringLength(100)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the price of the ticket.
    /// </summary>
    [Required, Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "Price must be greater than or equal to 0")]
    public decimal Price { get; init; }

    /// <summary>
    /// Gets or sets the total quantity of tickets available.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int TotalQuantity { get; init; }

    /// <summary>
    /// Gets or sets the description of the ticket type.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the date when ticket sales start.
    /// </summary>
    public DateTime? SaleStartDate { get; init; }

    /// <summary>
    /// Gets or sets the date when ticket sales end.
    /// </summary>
    public DateTime? SaleEndDate { get; init; }
}
