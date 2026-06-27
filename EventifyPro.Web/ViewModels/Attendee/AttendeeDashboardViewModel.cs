namespace EventifyPro.Web.ViewModels.Attendee;

public class AttendeeDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int TotalBookings { get; set; }
    public int TotalTickets { get; set; }
    public int TotalReviews { get; set; }
    public int ActiveTickets { get; set; }
    public int UsedTickets { get; set; }
    public int ExpiredTickets { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int ProfileCompletionPercentage { get; set; }
    public bool HasPhoneNumber { get; set; }
    public bool HasProfileImage { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public AttendeeUpcomingEventViewModel? UpcomingEvent { get; set; }
    public AttendeeReviewPromptViewModel? ReviewPrompt { get; set; }
    public List<AttendeeActivityViewModel> RecentActivity { get; set; } = [];
    public List<BookingSummaryViewModel> RecentBookings { get; set; } = [];
    public List<EventSummaryViewModel> RecommendedEvents { get; set; } = [];
}

public class AttendeeUpcomingEventViewModel
{
    public int EventId { get; set; }
    public int? TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime StartDate { get; set; }
    public int DaysRemaining { get; set; }
    public int HoursRemaining { get; set; }
    public int TicketCount { get; set; }
    public string BookingReference { get; set; } = string.Empty;
}

public class AttendeeReviewPromptViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
}

public class AttendeeActivityViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-circle";
    public string Tone { get; set; } = "neutral";
    public DateTime Date { get; set; }
}
