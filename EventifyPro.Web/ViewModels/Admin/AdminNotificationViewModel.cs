namespace EventifyPro.Web.ViewModels.Admin
{
    public class AdminNotificationViewModel
    {
        [Required]
        public bool IsSystemWide { get; set; } = true;

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? RecipientEmail { get; set; }

        [Required(ErrorMessage = "Notification type is required.")]
        public NotificationType Type { get; set; } = NotificationType.CustomAlert;

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Redirect URL cannot exceed 200 characters.")]
        public string? RedirectUrl { get; set; }

        public List<AdminSentNotificationViewModel> SentNotifications { get; set; } = new();
    }

    public class AdminSentNotificationViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RecipientCount { get; set; }
        public string Recipient { get; set; } = string.Empty;
    }
}
