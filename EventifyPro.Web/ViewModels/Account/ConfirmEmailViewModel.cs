namespace EventifyPro.Web.ViewModels.Account;

public class ConfirmEmailViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be exactly 6 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9]{6}$", ErrorMessage = "Verification code must be 6 alphanumeric characters.")]
    public string OtpCode { get; set; } = string.Empty;
}
