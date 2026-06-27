namespace EventifyPro.Web.ViewModels.Admin
{
    public class AdminSecurityViewModel
    {
        public bool RequireTwoFactorForAdmins { get; set; } = true;

        [Required(ErrorMessage = "Password minimum length is required.")]
        [Range(6, 128, ErrorMessage = "Password minimum length must be between 6 and 128 characters.")]
        public int PasswordMinLength { get; set; } = 8;

        public bool RequirePasswordUppercase { get; set; } = true;
        public bool RequirePasswordDigits { get; set; } = true;

        [Required(ErrorMessage = "Session timeout duration is required.")]
        [Range(5, 1440, ErrorMessage = "Session timeout must be between 5 and 1440 minutes (24 hours).")]
        public int SessionTimeoutMinutes { get; set; } = 30;

        [Required(ErrorMessage = "Maximum failed logins limit is required.")]
        [Range(1, 20, ErrorMessage = "Maximum failed logins must be between 1 and 20.")]
        public int MaxFailedLoginsBeforeLockout { get; set; } = 5;

        [Required(ErrorMessage = "Admin IP Whitelist is required. Use '*' to allow all.")]
        public string AdminIpWhitelist { get; set; } = "*"; // Allow all by default
    }
}
