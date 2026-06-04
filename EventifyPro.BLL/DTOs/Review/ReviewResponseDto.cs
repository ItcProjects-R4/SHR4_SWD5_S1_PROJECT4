namespace EventifyPro.BLL.DTOs.Review;

/// <summary>
/// Data transfer object containing review information.
/// </summary>
public record ReviewResponseDto
{
    /// <summary>
    /// Gets or sets the review identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the user identifier who wrote the review.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the user who wrote the review.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the event identifier being reviewed.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the title of the reviewed event.
    /// </summary>
    public string EventTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating value (1-5 stars).
    /// </summary>
    public byte Rating { get; init; }

    /// <summary>
    /// Gets or sets the review comment or detailed feedback.
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the review is hidden from public view.
    /// </summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the review was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the review was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}
