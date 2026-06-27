
namespace EventifyPro.Web.ViewModels.Organizer;

public class OrganizerWaitingListListViewModel
{
    public int? SelectedEventId { get; set; }
    public WaitingListStatus? SelectedStatus { get; set; }
    public string? SearchTerm { get; set; }
    
    // Summary Stats
    public int TotalWaiting { get; set; }
    public int TotalNotified { get; set; }
    public int TotalConverted { get; set; }
    public double ConversionRate { get; set; } // percentage of notified that converted

    public List<OrganizerWaitingListEntryViewModel> Entries { get; set; } = new();

    // Pagination attributes
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
}

public class OrganizerWaitingListEntryViewModel
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string TicketTypeName { get; set; } = string.Empty;
    public int QuantityWanted { get; set; }
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public string AttendeeInitials { get; set; } = string.Empty;
    public WaitingListStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int PositionInQueue { get; set; }
}
