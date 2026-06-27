namespace EventifyPro.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 4, ErrorMessage = "Username must be at least 4 characters long.")]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Username can contain letters, numbers, and underscores only.")]
    public string UserName { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = RoleNames.Attendee;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and privacy policy.")]
    public bool AcceptTerms { get; set; }

    // Organizer profile fields (dynamically shown/required on Register wizard)
    [Display(Name = "Organization Name")]
    [StringLength(200)]
    public string? OrganizationName { get; set; }

    [Display(Name = "Business Phone")]
    [StringLength(30)]
    [RegularExpression(@"^(01[0125]\d{8}|0[2345689]\d{7,8}|\+[1-9]\d{1,14})$", ErrorMessage = "Please enter a valid Egyptian mobile/landline number or international number starting with '+'.")]
    public string? BusinessPhone { get; set; }

    [Display(Name = "Website URL")]
    [StringLength(500)]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string? WebsiteUrl { get; set; }

    [Display(Name = "Commercial Register")]
    [StringLength(100)]
    public string? CommercialRegister { get; set; }

    [Display(Name = "Tax Number")]
    [StringLength(100)]
    public string? TaxNumber { get; set; }

    [Display(Name = "Organization Logo")]
    public IFormFile? LogoFile { get; set; }
}
