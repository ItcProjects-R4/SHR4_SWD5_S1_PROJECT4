namespace EventifyPro.BLL.DTOs.Review;

/// <summary>
/// Data transfer object for creating an event review.
/// </summary>
public record ReviewCreateDto
{
    /// <summary>
    /// Gets or sets the event identifier to review.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the rating value (1-5 stars).
    /// </summary>
    [Required, Range(1, 5)]
    public byte Rating { get; init; }

    /// <summary>
    /// Gets or sets the review comment or detailed feedback.
    /// </summary>
    [StringLength(1000)]
    public string? Comment { get; init; }
}
