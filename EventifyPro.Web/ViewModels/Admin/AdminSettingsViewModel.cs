namespace EventifyPro.Web.ViewModels.Admin
{
    public class AdminSettingsViewModel
    {
        [Required(ErrorMessage = "Platform Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Platform Name must be between 2 and 100 characters.")]
        public string PlatformName { get; set; } = "Eventify Pro";

        [Required(ErrorMessage = "Support Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(256, ErrorMessage = "Support Email cannot exceed 256 characters.")]
        public string SupportEmail { get; set; } = "support@eventifypro.com";

        [Required(ErrorMessage = "Support WhatsApp number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        [StringLength(20, ErrorMessage = "Support WhatsApp number cannot exceed 20 characters.")]
        public string SupportWhatsApp { get; set; } = "01064665247";

        [Required(ErrorMessage = "Ticket Commission Rate is required.")]
        [Range(0.00, 100.00, ErrorMessage = "Ticket Commission Rate must be between 0.00% and 100.00%.")]
        public decimal TicketCommissionRate { get; set; } = 5.0m; // 5% default

        public bool EnableMaintenanceMode { get; set; } = false;

        [Required(ErrorMessage = "Maximum tickets per booking is required.")]
        [Range(1, 100, ErrorMessage = "Maximum tickets per booking must be between 1 and 100.")]
        public int MaxTicketsPerBooking { get; set; } = 10;

        [Required(ErrorMessage = "Allowed Upload Extensions are required.")]
        [StringLength(200, ErrorMessage = "Allowed Upload Extensions cannot exceed 200 characters.")]
        public string AllowedUploadExtensions { get; set; } = ".jpg,.jpeg,.png,.pdf";

        public string GeminiApiKey { get; set; } = string.Empty;
    }
}
