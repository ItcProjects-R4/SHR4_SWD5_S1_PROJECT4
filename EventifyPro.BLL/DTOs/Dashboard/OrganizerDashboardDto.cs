namespace EventifyPro.BLL.DTOs.Dashboard;

/// <summary>
/// Data transfer object containing comprehensive dashboard information for an event organizer.
/// </summary>
public record OrganizerDashboardDto
{
    /// <summary>
    /// Gets or sets the total number of events created by the organizer.
    /// </summary>
    public int TotalEvents { get; init; }

    /// <summary>
    /// Gets or sets the number of currently active events.
    /// </summary>
    public int ActiveEvents { get; init; }

    /// <summary>
    /// Gets or sets the total number of bookings across all events.
    /// </summary>
    public int TotalBookings { get; init; }

    /// <summary>
    /// Gets or sets the total revenue generated from all events.
    /// </summary>
    public decimal TotalRevenue { get; init; }

    /// <summary>
    /// Gets or sets the average rating of all events.
    /// </summary>
    public double AverageEventRating { get; init; }

    /// <summary>
    /// Gets or sets the total number of attendees across all events.
    /// </summary>
    public int TotalAttendees { get; init; }

    /// <summary>
    /// Gets or sets the list of top-performing events by sales.
    /// </summary>
    public List<EventSalesDto> TopEvents { get; init; } = [];

    /// <summary>
    /// Gets or sets the breakdown of booking statuses.
    /// </summary>
    public Dictionary<string, int> BookingStatusBreakdown { get; init; } = [];

    /// <summary>
    /// Gets or sets the revenue data grouped by month.
    /// </summary>
    public Dictionary<string, decimal> RevenueByMonth { get; init; } = [];

    /// <summary>
    /// Gets or sets the number of attendees waiting on waiting list.
    /// </summary>
    public int WaitingListCount { get; init; }
}
