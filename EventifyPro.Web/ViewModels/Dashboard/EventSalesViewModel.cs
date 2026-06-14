namespace EventifyPro.Web.ViewModels.Dashboard;

public class EventSalesViewModel
{
    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public int TotalBookings { get; set; }

    public int TotalTicketsSold { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal AverageTicketPrice { get; set; }

    public DateTime StartDate { get; set; }
}
