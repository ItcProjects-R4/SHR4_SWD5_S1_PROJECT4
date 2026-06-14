using System.ComponentModel.DataAnnotations;

namespace EventifyPro.Web.ViewModels.Feedback;

public class FeedbackViewModel
{
    [StringLength(100)]
    public string? Name { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Required]
    [StringLength(1500)]
    public string Message { get; set; } = string.Empty;
}
