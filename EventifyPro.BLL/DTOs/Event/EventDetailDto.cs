namespace EventifyPro.BLL.DTOs.Event;

/// <summary>
/// Data transfer object containing complete event details with all related information.
/// </summary>
public record EventDetailDto
{
    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the event title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the event description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the event start date and time.
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Gets or sets the event end date and time.
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Gets or sets the event location/venue address.
    /// </summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the city where the event takes place.
    /// </summary>
    public string City { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to the event image.
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// Gets or sets the current status of the event (e.g., Upcoming, Ongoing, Completed, Cancelled).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public string? ReviewNotes { get; init; }

    public string? ReviewedByAdminId { get; init; }

    public DateTime? ReviewedAt { get; init; }

    /// <summary>
    /// Gets or sets the maximum capacity of the event.
    /// </summary>
    public int? MaxCapacity { get; init; }

    /// <summary>
    /// Gets or sets the organizer's user identifier.
    /// </summary>
    public string OrganizerId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the event organizer.
    /// </summary>
    public string OrganizerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    public int CategoryId { get; init; }

    /// <summary>
    /// Gets or sets the name of the event category.
    /// </summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the event was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the event was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets or sets the total number of bookings for the event.
    /// </summary>
    public int TotalBookings { get; init; }

    /// <summary>
    /// Gets or sets the total number of tickets sold.
    /// </summary>
    public int TotalTicketsSold { get; init; }

    /// <summary>
    /// Gets or sets the total revenue generated from the event.
    /// </summary>
    public decimal TotalRevenue { get; init; }

    /// <summary>
    /// Gets or sets the average rating of the event.
    /// </summary>
    public double AverageRating { get; init; }
}
