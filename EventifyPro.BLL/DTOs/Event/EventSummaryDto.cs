namespace EventifyPro.BLL.DTOs.Event;

/// <summary>
/// Data transfer object for lightweight event summary information used in lists.
/// </summary>
public record EventSummaryDto
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
    /// Gets or sets the event start date and time.
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Gets or sets the event end date and time.
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Gets or sets the city where the event takes place.
    /// </summary>
    public string City { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to the event image.
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// Gets or sets the current status of the event.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public string? ReviewNotes { get; init; }

    public DateTime? ReviewedAt { get; init; }

    /// <summary>
    /// Gets or sets the maximum capacity of the event.
    /// </summary>
    public int? MaxCapacity { get; init; }

    /// <summary>
    /// Gets or sets the name of the event organizer.
    /// </summary>
    public string OrganizerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the event category.
    /// </summary>
    public string CategoryName { get; init; } = string.Empty;
    public decimal MinPrice { get; init; }
    public string Location { get; init; } = string.Empty;
    public decimal MinTicketPrice { get; init; }
    public decimal MaxTicketPrice { get; init; }
}
