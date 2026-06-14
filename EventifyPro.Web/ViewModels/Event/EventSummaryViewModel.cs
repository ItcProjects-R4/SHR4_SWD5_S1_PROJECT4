namespace EventifyPro.Web.ViewModels.Event;

public class EventSummaryViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string City { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ReviewNotes { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int? MaxCapacity { get; set; }

    public string OrganizerName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
}
