namespace EventifyPro.BLL.DTOs.Dashboard;

public record RecentUserDto
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Role { get; init; } = "Attendee";
}

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
    public List<RecentUserDto> RecentUsers { get; init; } = [];
    public Dictionary<int, decimal> CurrentMonthWeeklyRevenue { get; init; } = [];
    public Dictionary<int, decimal> LastMonthWeeklyRevenue { get; init; } = [];
}
