namespace EventifyPro.BLL.DTOs.Review;

public record OrganizerReviewItemDto
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string AttendeeName { get; init; } = string.Empty;
    public string AttendeeEmail { get; init; } = string.Empty;
    public string AttendeeInitials { get; init; } = string.Empty;
    public byte Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? OrganizerReply { get; init; }
    public DateTime? RepliedAt { get; init; }
    public bool IsFlagged { get; init; }
    public string? FlaggedReason { get; init; }
}
