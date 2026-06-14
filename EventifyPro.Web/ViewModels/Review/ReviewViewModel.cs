namespace EventifyPro.Web.ViewModels.Review;

public class ReviewViewModel
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
