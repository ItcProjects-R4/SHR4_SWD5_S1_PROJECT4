namespace EventifyPro.BLL.DTOs.Event;

/// <summary>
/// Data transfer object for filtering and searching events.
/// </summary>
public record EventFilterDto
{
    /// <summary>
    /// Gets or sets the event title to filter by.
    /// </summary>
    [StringLength(200)]
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets the category identifier to filter by.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets or sets the city to filter events by.
    /// </summary>
    [StringLength(100)]
    public string? City { get; init; }

    /// <summary>
    /// Gets or sets the start date range (from date).
    /// </summary>
    public DateTime? StartDateFrom { get; init; }

    /// <summary>
    /// Gets or sets the start date range (to date).
    /// </summary>
    public DateTime? StartDateTo { get; init; }

    /// <summary>
    /// Gets or sets the event status to filter by.
    /// </summary>
    [StringLength(50)]
    public string? Status { get; init; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Gets or sets the field to sort results by.
    /// </summary>
    [StringLength(50)]
    public string? SortBy { get; init; } = "StartDate";

    /// <summary>
    /// Gets or sets a value indicating whether to sort in descending order.
    /// </summary>
    public bool IsDescending { get; init; }
}
