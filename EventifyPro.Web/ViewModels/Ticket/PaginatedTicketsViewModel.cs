
namespace EventifyPro.Web.ViewModels.Ticket;

public class PaginatedTicketsViewModel
{
    public List<TicketViewModel> Tickets { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string CurrentStatus { get; set; } = "active";
    public string SearchQuery { get; set; } = "";
    
    public int ActiveCount { get; set; }
    public int UsedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int CancelledCount { get; set; }
}
