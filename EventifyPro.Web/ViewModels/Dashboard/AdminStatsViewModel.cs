namespace EventifyPro.Web.ViewModels.Dashboard;

public class AdminStatsViewModel
{
    public int TotalUsers { get; set; }

    public int TotalEvents { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TotalBookings { get; set; }

    public int TotalTickets { get; set; }

    public double AverageReviewRating { get; set; }

    public Dictionary<string, int> UsersByRole { get; set; } = [];

    public Dictionary<string, decimal> RevenueByMonth { get; set; } = [];
}
