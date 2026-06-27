namespace EventifyPro.BLL.DTOs.Admin
{
    public class AdminSentNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RecipientCount { get; set; }
        public string Recipient { get; set; } = string.Empty;
    }
}
