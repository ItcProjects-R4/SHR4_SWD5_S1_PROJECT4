namespace EventifyPro.Web.ViewModels.Event;

public class EventDetailViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ReviewNotes { get; set; }

    public string? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int? MaxCapacity { get; set; }

    public string OrganizerId { get; set; } = string.Empty;

    public string OrganizerName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int TotalBookings { get; set; }

    public int TotalTicketsSold { get; set; }

    public decimal TotalRevenue { get; set; }

    public double AverageRating { get; set; }
    public bool IsSaved { get; set; }
}
