

namespace EventifyPro.Web.ViewModels.Organizer
{
    public class OrganizerProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? PhoneNumber { get; set; }

        public string? ProfileImageUrl { get; set; }

        public IFormFile? ProfilePicture { get; set; }

        // Business/Organization details
        [StringLength(150, ErrorMessage = "Organization name cannot exceed 150 characters.")]
        public string? OrganizationName { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string? Bio { get; set; }

        [Url(ErrorMessage = "Invalid website URL.")]
        [StringLength(200, ErrorMessage = "Website URL cannot exceed 200 characters.")]
        public string? WebsiteUrl { get; set; }

        [Url(ErrorMessage = "Invalid Facebook URL.")]
        [StringLength(200, ErrorMessage = "Facebook URL cannot exceed 200 characters.")]
        public string? FacebookUrl { get; set; }

        [Url(ErrorMessage = "Invalid LinkedIn URL.")]
        [StringLength(200, ErrorMessage = "LinkedIn URL cannot exceed 200 characters.")]
        public string? LinkedInUrl { get; set; }

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
