namespace EventifyPro.Web.ViewModels.Dashboard;

public class OrganizerDashboardViewModel
{
    public int TotalEvents { get; set; }

    public int ActiveEvents { get; set; }

    public int TotalBookings { get; set; }

    public decimal TotalRevenue { get; set; }

    public double AverageEventRating { get; set; }

    public int TotalAttendees { get; set; }

    public IReadOnlyList<EventSalesViewModel> TopEvents { get; set; } = [];

    public Dictionary<string, int> BookingStatusBreakdown { get; set; } = [];

    public Dictionary<string, decimal> RevenueByMonth { get; set; } = [];
}
