namespace EventifyPro.Web.ViewModels.Event;

public class EventPerformanceViewModel
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalTicketsSold { get; set; }
    public int TotalCapacity { get; set; }
    public double SoldPercentage { get; set; }
    public int WaitingListCount { get; set; }
    public int ConfirmedBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CancelledBookings { get; set; }
    public List<TicketTypePerformanceViewModel> TicketTypes { get; set; } = new();
}

public class TicketTypePerformanceViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public double SoldPercentage { get; set; }
}
