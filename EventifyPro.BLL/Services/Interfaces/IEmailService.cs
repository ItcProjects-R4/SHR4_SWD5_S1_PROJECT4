namespace EventifyPro.BLL.Services.Interfaces;

public interface IEmailService
{
    Task SendBookingConfirmationAsync(string recipientEmail, string recipientName, string bookingRef, byte[] pdfAttachment, CancellationToken cancellationToken = default);
    Task SendCancellationAsync(string recipientEmail, string recipientName, string eventTitle, string reason, CancellationToken cancellationToken = default);
    Task SendWaitingListNotificationAsync(string recipientEmail, string recipientName, string eventTitle, string ticketTypeName, string claimUrl, DateTime expiresAt, CancellationToken cancellationToken = default);
}
