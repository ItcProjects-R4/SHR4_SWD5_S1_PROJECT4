namespace EventifyPro.BLL.Services.Interfaces;

public interface IEmailService
{
    // Original methods
    Task SendBookingConfirmationAsync(string recipientEmail, string recipientName, string bookingRef, byte[] pdfAttachment, CancellationToken cancellationToken = default);
    Task SendCancellationAsync(string recipientEmail, string recipientName, string eventTitle, string reason, CancellationToken cancellationToken = default);
    Task SendWaitingListNotificationAsync(string recipientEmail, string recipientName, string eventTitle, string ticketTypeName, string claimUrl, DateTime expiresAt, CancellationToken cancellationToken = default);

    // New 12 requested email scenarios
    Task SendWelcomeEmailAsync(string recipientEmail, string recipientName, CancellationToken cancellationToken = default);
    
    Task SendWeeklyDigestEmailAsync(
        string recipientEmail, 
        string recipientName, 
        List<(string Title, string Category, DateTime StartDate, string Location, string Url)> events, 
        CancellationToken cancellationToken = default);

    Task SendEventReminderEmailAsync(
        string recipientEmail, 
        string recipientName, 
        string eventTitle, 
        DateTime startDate, 
        string location, 
        int hoursRemaining, 
        CancellationToken cancellationToken = default);

    Task SendEventUpdatedEmailAsync(
        string recipientEmail, 
        string recipientName, 
        string eventTitle, 
        string updatedDetails, 
        string changeLog, 
        CancellationToken cancellationToken = default);

    Task SendRefundConfirmationAsync(
        string recipientEmail, 
        string recipientName, 
        string bookingRef, 
        decimal amount, 
        DateTime refundDate, 
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string recipientEmail, 
        string recipientName, 
        string otpCode, 
        CancellationToken cancellationToken = default);

    Task SendEmailVerificationAsync(
        string recipientEmail, 
        string recipientName, 
        string otpCode, 
        CancellationToken cancellationToken = default);

    Task SendSecurityNotificationAsync(
        string recipientEmail, 
        string recipientName, 
        string securityAction, 
        string details, 
        CancellationToken cancellationToken = default);

    // Organizer Emails
    Task SendOrganizerAccountActivatedAsync(string recipientEmail, string recipientName, CancellationToken cancellationToken = default);
    Task SendOrganizerEventApprovedAsync(string recipientEmail, string recipientName, string eventTitle, CancellationToken cancellationToken = default);
    Task SendOrganizerEventRejectedAsync(string recipientEmail, string recipientName, string eventTitle, string reason, CancellationToken cancellationToken = default);
    Task SendOrganizerEventPublishedAsync(string recipientEmail, string recipientName, string eventTitle, CancellationToken cancellationToken = default);
    Task SendOrganizerEventSoldOutAsync(string recipientEmail, string recipientName, string eventTitle, int totalTickets, CancellationToken cancellationToken = default);
    Task SendPayoutStatusEmailAsync(
        string recipientEmail, 
        string recipientName, 
        decimal amount, 
        string status, 
        string? referenceNumber, 
        string? notes, 
        CancellationToken cancellationToken = default);

    // Scanner Credentials
    Task SendScannerCredentialsEmailAsync(
        string recipientEmail, 
        string recipientName, 
        string temporaryPassword, 
        string organizerName, 
        CancellationToken cancellationToken = default);

    // Post Event Feedback
    Task SendPostEventFeedbackAsync(
        string recipientEmail, 
        string recipientName, 
        string eventTitle, 
        int eventId, 
        string feedbackUrl, 
        CancellationToken cancellationToken = default);

    // Generic Sending Method
    Task SendEmailAsync(
        string to, 
        string subject, 
        string htmlBody, 
        byte[]? attachment = null, 
        string? attachmentName = null, 
        CancellationToken cancellationToken = default);
}

