namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a user review for an attended event.
/// Contains rating, comment, and visibility control for moderation purposes.
/// </summary>
/// <remarks>
/// Reviews allow attendees to provide feedback on events through a rating system and optional comments.
/// Reviews can be hidden by moderators for content policy violations. Multiple reviews per user-event
/// combination are prevented through unique constraints at the database level.
/// </remarks>
public class Review : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the review.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the reviewer (foreign key).
    /// </summary>
    /// <value>The reviewer's user ID.</value>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event ID being reviewed (foreign key).
    /// </summary>
    /// <value>The event identifier.</value>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the rating given by the reviewer.
    /// </summary>
    /// <value>The rating score (typically 1-5).</value>
    public byte Rating { get; set; }

    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the review is hidden from public display.
    /// </summary>
    /// <value><c>true</c> if hidden by moderators; otherwise, <c>false</c>.</value>
    public bool IsHidden { get; set; } = false;

    public string? HiddenById { get; set; }
    public string? HiddenReason { get; set; }

    // Organizer reply and reporting fields
    public string? OrganizerReply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public bool IsFlagged { get; set; } = false;
    public string? FlaggedReason { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public ApplicationUser? HiddenBy { get; set; }
}