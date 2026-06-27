namespace EventifyPro.BLL.DTOs.SavedEvent;

public class SavedEventDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int EventId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Flattened event details for display
    public string EventTitle { get; set; } = string.Empty;
    public string EventLocation { get; set; } = string.Empty;
    public string EventCity { get; set; } = string.Empty;
    public string? EventImageUrl { get; set; }
    public DateTime EventStartDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public string EventCategoryName { get; set; } = string.Empty;
    public string EventOrganizerName { get; set; } = string.Empty;
}
