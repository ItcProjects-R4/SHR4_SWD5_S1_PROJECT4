

namespace EventifyPro.Web.ViewModels.Organizer;

public class OrganizerReviewsListViewModel
{
    public string? SearchTerm { get; set; }
    public int? RatingFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    // Summary Stats
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();

    public List<OrganizerReviewItemViewModel> Reviews { get; set; } = new();
    
    // Pagination attributes
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
}

public class OrganizerReviewItemViewModel
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public string AttendeeInitials { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? OrganizerReply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public bool IsFlagged { get; set; }
    public string? FlaggedReason { get; set; }
}
