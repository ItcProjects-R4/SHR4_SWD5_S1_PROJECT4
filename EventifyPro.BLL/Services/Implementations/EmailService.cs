namespace EventifyPro.BLL.Services.Implementations;

public class EmailService : IEmailService
{
    public EmailService()
    {
    }

    public Task SendBookingConfirmationAsync(string recipientEmail, string recipientName, string bookingRef, byte[] pdfAttachment, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task SendCancellationAsync(string recipientEmail, string recipientName, string eventTitle, string reason, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task SendWaitingListNotificationAsync(string recipientEmail, string recipientName, string eventTitle, string ticketTypeName, string claimUrl, DateTime expiresAt, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
