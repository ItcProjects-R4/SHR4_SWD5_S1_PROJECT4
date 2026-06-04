namespace EventifyPro.Web.ViewModels;

public class AdminFeedbackItemViewModel
{
    public int Id { get; set; }

    public string DisplayName { get; set; } = "Eventify Pro User";

    public string? Email { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; }
}
