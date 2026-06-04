namespace EventifyPro.BLL.DTOs.Event;

/// <summary>
/// Data transfer object for updating an existing event.
/// </summary>
public record EventUpdateDto
{
    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    [Required]
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the event title.
    /// </summary>
    [Required, StringLength(200)]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the event description.
    /// </summary>
    [Required, StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the event start date and time.
    /// </summary>
    [Required]
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Gets or sets the event end date and time.
    /// </summary>
    [Required]
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Gets or sets the event location/venue address.
    /// </summary>
    [Required, StringLength(300)]
    public string Location { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the city where the event takes place.
    /// </summary>
    [Required, StringLength(100)]
    public string City { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to the event image.
    /// </summary>
    [StringLength(500)]
    public string? ImageUrl { get; init; }

    /// <summary>
    /// Gets or sets the maximum capacity of the event.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Max capacity must be greater than 0")]
    public int? MaxCapacity { get; init; }

    /// <summary>
    /// Gets or sets the category identifier for the event.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
}
