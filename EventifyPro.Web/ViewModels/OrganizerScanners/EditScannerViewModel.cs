

namespace EventifyPro.Web.ViewModels.OrganizerScanners;

public class EditScannerViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(200, ErrorMessage = "Full name cannot exceed 200 characters.")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty; // Read-only in UI

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}
