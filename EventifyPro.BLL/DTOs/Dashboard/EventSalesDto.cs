namespace EventifyPro.BLL.DTOs.Dashboard;

/// <summary>
/// Data transfer object containing sales information for an event on the dashboard.
/// </summary>
public record EventSalesDto
{
    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the title of the event.
    /// </summary>
    public string EventTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of bookings for the event.
    /// </summary>
    public int TotalBookings { get; init; }

    /// <summary>
    /// Gets or sets the total number of tickets sold for the event.
    /// </summary>
    public int TotalTicketsSold { get; init; }

    /// <summary>
    /// Gets or sets the total revenue generated from the event.
    /// </summary>
    public decimal TotalRevenue { get; init; }

    /// <summary>
    /// Gets or sets the average price per ticket sold.
    /// </summary>
    public decimal AverageTicketPrice { get; init; }

    /// <summary>
    /// Gets or sets the start date and time of the event.
    /// </summary>
    public DateTime StartDate { get; init; }
}
