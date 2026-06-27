
namespace EventifyPro.Web.ViewModels.OrganizerScanners;

public class CreateScannerViewModel
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MinLength(8), DataType(DataType.Password)]
    public string? Password { get; set; }
}
