namespace EventifyPro.Web.ViewModels.Attendee;

public class AttendeeProfileViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid Phone Number.")]
    [StringLength(20, ErrorMessage = "Phone Number cannot exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    public string? ProfileImageUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, ErrorMessage = "The password must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordWithOtpViewModel
{
    [Required(ErrorMessage = "OTP code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be exactly 6 digits.")]
    [Display(Name = "OTP Verification Code")]
    public string OtpCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, ErrorMessage = "The password must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
