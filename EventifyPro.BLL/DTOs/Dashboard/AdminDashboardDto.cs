namespace EventifyPro.BLL.DTOs.Dashboard;

public record AdminDashboardDto
{
    public int TotalUsers { get; init; }
    public int TotalEvents { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalBookings { get; init; }
    public int TotalTickets { get; init; }
    public double AverageReviewRating { get; init; }
    public Dictionary<string, int> UsersByRole { get; init; } = [];
    public Dictionary<string, decimal> RevenueByMonth { get; init; } = [];
}
