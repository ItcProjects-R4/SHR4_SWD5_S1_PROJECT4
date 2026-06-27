namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a category of tickets for an event with pricing and availability.
/// Supports sale date ranges, quantity tracking, and optimistic concurrency control.
/// </summary>
/// <remarks>
/// TicketType defines a specific category of tickets within an event (e.g., VIP, Standard, Early Bird).
/// Each type has its own pricing, quantity limit, and optional sale period. The RowVersion property
/// supports optimistic concurrency to prevent overselling of tickets.
/// </remarks>
public class TicketType : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the ticket type.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the event ID (foreign key).
    /// </summary>
    /// <value>The event identifier.</value>
    public int EventId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the price per ticket of this type.
    /// </summary>
    /// <value>The ticket price.</value>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the total quantity of tickets available for this type.
    /// </summary>
    /// <value>The total ticket count.</value>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Gets or sets the number of tickets already sold.
    /// </summary>
    /// <value>The count of sold tickets.</value>
    public int SoldQuantity { get; set; } = 0;

    public string? Description { get; set; }
    public DateTime? SaleStartDate { get; set; }
    public DateTime? SaleEndDate { get; set; }

    /// <summary>
    /// Gets or sets the optimistic concurrency token for preventing overselling.
    /// </summary>
    /// <value>The row version used for concurrency control.</value>
    public byte[] RowVersion { get; set; } = [];

    // Navigation
    public Event Event { get; set; } = null!;
    public ICollection<BookingItem> BookingItems { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<WaitingList> WaitingListEntries { get; set; } = [];
}