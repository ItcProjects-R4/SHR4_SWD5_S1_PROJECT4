using System.ComponentModel.DataAnnotations;
using Eventify.Domain.Constants;

namespace EventifyPro.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_\\.]+$", ErrorMessage = "Username can contain letters, numbers, dots, and underscores only.")]
    public string UserName { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = RoleNames.Attendee;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and privacy policy.")]
    public bool AcceptTerms { get; set; }
}
