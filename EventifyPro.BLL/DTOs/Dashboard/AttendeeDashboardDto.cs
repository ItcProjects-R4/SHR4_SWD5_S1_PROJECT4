namespace EventifyPro.BLL.DTOs.Dashboard;

/// <summary>
/// Data transfer object containing comprehensive dashboard information for an event attendee.
/// </summary>
public record AttendeeDashboardDto
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public int TotalBookings { get; init; }
    public int TotalTickets { get; init; }
    public int TotalReviews { get; init; }
    public int ActiveTickets { get; init; }
    public int UsedTickets { get; init; }
    public int ExpiredTickets { get; init; }
    public int PendingBookings { get; init; }
    public int ConfirmedBookings { get; init; }
    public int ProfileCompletionPercentage { get; init; }
    public bool HasPhoneNumber { get; init; }
    public bool HasProfileImage { get; init; }
    public bool IsEmailConfirmed { get; init; }

    public AttendeeUpcomingEventDto? UpcomingEvent { get; init; }
    public AttendeeReviewPromptDto? ReviewPrompt { get; init; }
    public List<AttendeeActivityDto> RecentActivity { get; init; } = [];
    public List<AttendeeBookingSummaryDto> RecentBookings { get; init; } = [];
    public List<AttendeeRecommendedEventDto> RecommendedEvents { get; init; } = [];
}

public record AttendeeUpcomingEventDto
{
    public int EventId { get; init; }
    public int? TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateTime StartDate { get; init; }
    public int DaysRemaining { get; init; }
    public int HoursRemaining { get; init; }
    public int TicketCount { get; init; }
    public string BookingReference { get; init; } = string.Empty;
}

public record AttendeeReviewPromptDto
{
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
}

public record AttendeeActivityDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "fa-circle";
    public string Tone { get; init; } = "neutral";
    public DateTime Date { get; init; }
}

public record AttendeeBookingSummaryDto
{
    public int Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string BookingReference { get; init; } = string.Empty;
    public DateTime BookingDate { get; init; }
}

public record AttendeeRecommendedEventDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal MinPrice { get; init; }
    public string OrganizerName { get; init; } = string.Empty;
}
