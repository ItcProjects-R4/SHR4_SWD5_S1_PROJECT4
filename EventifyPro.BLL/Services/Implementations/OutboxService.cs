namespace EventifyPro.BLL.Services.Implementations;

public class OutboxService : IOutboxService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public OutboxService(IUnitOfWork unitOfWork, IEmailService emailService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _mapper = mapper;
    }

    public Task EnqueueAsync(string type, object payload, CancellationToken cancellationToken = default) =>
        EnqueueAsync(type, payload, scheduledFor: null, cancellationToken);

    public async Task EnqueueAsync(string type, object payload, DateTime? scheduledFor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Outbox message type cannot be null or empty.", nameof(type));
        }

        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        var jsonPayload = JsonSerializer.Serialize(payload);
        
        var outboxMessage = new OutboxMessage
        {
            Type = type,
            Payload = jsonPayload,
            CreatedAt = DateTime.UtcNow,
            ScheduledFor = scheduledFor,
            ProcessedAt = null,
            RetryCount = 0,
            LastError = null
        };

        await _unitOfWork.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _unitOfWork.OutboxMessages.GetMessagesForRetryAsync(maxRetries: 3, cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                // To prevent double processing by other workers in parallel, we can increment retry count or mark temporary state,
                // but since it's a BackgroundService, we will do it within a transaction.
                
                await ProcessMessagePayloadAsync(message, cancellationToken);
                
                // Fetch the entity in tracking context to modify state
                var trackedMessage = await _unitOfWork.OutboxMessages.GetByIdAsync(message.Id, cancellationToken);
                if (trackedMessage != null)
                {
                    trackedMessage.ProcessedAt = DateTime.UtcNow;
                    trackedMessage.LastError = null;
                    _unitOfWork.OutboxMessages.Update(trackedMessage);
                }
            }
            catch (Exception ex)
            {
                var trackedMessage = await _unitOfWork.OutboxMessages.GetByIdAsync(message.Id, cancellationToken);
                if (trackedMessage != null)
                {
                    trackedMessage.RetryCount += 1;
                    trackedMessage.LastError = $"{ex.Message} \n {ex.StackTrace}";
                    _unitOfWork.OutboxMessages.Update(trackedMessage);
                }
            }
            
            // Save after each message processing to ensure partial successes are stored
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessMessagePayloadAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        switch (message.Type)
        {
            case "Email.Welcome":
                var welcome = JsonSerializer.Deserialize<WelcomePayload>(message.Payload, options);
                if (welcome != null)
                {
                    await _emailService.SendWelcomeEmailAsync(welcome.RecipientEmail, welcome.RecipientName, cancellationToken);
                }
                break;

            case "Email.Verification":
                var verify = JsonSerializer.Deserialize<VerificationPayload>(message.Payload, options);
                if (verify != null)
                {
                    await _emailService.SendEmailVerificationAsync(verify.RecipientEmail, verify.RecipientName, verify.VerificationLink, cancellationToken);
                }
                break;

            case "Email.PasswordReset":
                var pwdReset = JsonSerializer.Deserialize<PasswordResetPayload>(message.Payload, options);
                if (pwdReset != null)
                {
                    await _emailService.SendPasswordResetAsync(pwdReset.RecipientEmail, pwdReset.RecipientName, pwdReset.ResetLink, cancellationToken);
                }
                break;

            case "Email.SecurityNotification":
                var secNotification = JsonSerializer.Deserialize<SecurityNotificationPayload>(message.Payload, options);
                if (secNotification != null)
                {
                    await _emailService.SendSecurityNotificationAsync(secNotification.RecipientEmail, secNotification.RecipientName, secNotification.SecurityAction, secNotification.Details, cancellationToken);
                }
                break;

            case "Email.TicketConfirmation":
                var ticketConf = JsonSerializer.Deserialize<TicketConfirmationPayload>(message.Payload, options);
                if (ticketConf != null)
                {
                    byte[]? attachment = null;
                    if (!string.IsNullOrEmpty(ticketConf.PdfAttachmentBase64))
                    {
                        attachment = Convert.FromBase64String(ticketConf.PdfAttachmentBase64);
                    }
                    
                    await _emailService.SendBookingConfirmationAsync(
                        ticketConf.RecipientEmail, 
                        ticketConf.RecipientName, 
                        ticketConf.BookingRef, 
                        attachment ?? [], 
                        cancellationToken);
                }
                break;

            case "Email.EventReminder":
                var reminder = JsonSerializer.Deserialize<EventReminderPayload>(message.Payload, options);
                if (reminder != null)
                {
                    await _emailService.SendEventReminderEmailAsync(
                        reminder.RecipientEmail, 
                        reminder.RecipientName, 
                        reminder.EventTitle, 
                        reminder.StartDate, 
                        reminder.Location, 
                        reminder.HoursRemaining, 
                        cancellationToken);
                }
                break;

            case "Email.EventUpdated":
                var updated = JsonSerializer.Deserialize<EventUpdatedPayload>(message.Payload, options);
                if (updated != null)
                {
                    await _emailService.SendEventUpdatedEmailAsync(
                        updated.RecipientEmail, 
                        updated.RecipientName, 
                        updated.EventTitle, 
                        updated.UpdatedDetails, 
                        updated.ChangeLog, 
                        cancellationToken);
                }
                break;

            case "Email.EventCancelled":
                var cancelled = JsonSerializer.Deserialize<EventCancelledPayload>(message.Payload, options);
                if (cancelled != null)
                {
                    await _emailService.SendCancellationAsync(
                        cancelled.RecipientEmail, 
                        cancelled.RecipientName, 
                        cancelled.EventTitle, 
                        cancelled.Reason, 
                        cancellationToken);
                }
                break;

            case "Email.RefundConfirmation":
                var refund = JsonSerializer.Deserialize<RefundConfirmationPayload>(message.Payload, options);
                if (refund != null)
                {
                    await _emailService.SendRefundConfirmationAsync(
                        refund.RecipientEmail, 
                        refund.RecipientName, 
                        refund.BookingRef, 
                        refund.Amount, 
                        refund.RefundDate, 
                        cancellationToken);
                }
                break;

            case "Email.WeeklyDigest":
                var digest = JsonSerializer.Deserialize<WeeklyDigestPayload>(message.Payload, options);
                if (digest != null)
                {
                    var typedEvents = new List<(string Title, string Category, DateTime StartDate, string Location, string Url)>();
                    if (digest.Events != null)
                    {
                        foreach (var e in digest.Events)
                        {
                            typedEvents.Add((e.Title, e.Category, e.StartDate, e.Location, e.Url));
                        }
                    }
                    await _emailService.SendWeeklyDigestEmailAsync(digest.RecipientEmail, digest.RecipientName, typedEvents, cancellationToken);
                }
                break;

            case "Email.OrganizerEventApproved":
                var approved = JsonSerializer.Deserialize<OrganizerEmailPayload>(message.Payload, options);
                if (approved != null)
                {
                    await _emailService.SendOrganizerEventApprovedAsync(approved.RecipientEmail, approved.RecipientName, approved.EventTitle, cancellationToken);
                }
                break;

            case "Email.OrganizerEventRejected":
                var rejected = JsonSerializer.Deserialize<OrganizerEmailWithReasonPayload>(message.Payload, options);
                if (rejected != null)
                {
                    await _emailService.SendOrganizerEventRejectedAsync(rejected.RecipientEmail, rejected.RecipientName, rejected.EventTitle, rejected.Reason, cancellationToken);
                }
                break;

            case "Email.OrganizerEventPublished":
                var published = JsonSerializer.Deserialize<OrganizerEmailPayload>(message.Payload, options);
                if (published != null)
                {
                    await _emailService.SendOrganizerEventPublishedAsync(published.RecipientEmail, published.RecipientName, published.EventTitle, cancellationToken);
                }
                break;

            case "Email.OrganizerEventSoldOut":
                var soldOut = JsonSerializer.Deserialize<OrganizerEmailSoldOutPayload>(message.Payload, options);
                if (soldOut != null)
                {
                    await _emailService.SendOrganizerEventSoldOutAsync(soldOut.RecipientEmail, soldOut.RecipientName, soldOut.EventTitle, soldOut.TotalTickets, cancellationToken);
                }
                break;

            case "Email.PostEventFeedback":
                var feedback = JsonSerializer.Deserialize<PostEventFeedbackPayload>(message.Payload, options);
                if (feedback != null)
                {
                    await _emailService.SendPostEventFeedbackAsync(
                        feedback.RecipientEmail, 
                        feedback.RecipientName, 
                        feedback.EventTitle, 
                        feedback.EventId, 
                        feedback.FeedbackUrl, 
                        cancellationToken);
                }
                break;

            case "Email.WaitingListNotification":
                var waiting = JsonSerializer.Deserialize<WaitingListNotificationPayload>(message.Payload, options);
                if (waiting != null)
                {
                    await _emailService.SendWaitingListNotificationAsync(
                        waiting.RecipientEmail, 
                        waiting.RecipientName, 
                        waiting.EventTitle, 
                        waiting.TicketTypeName, 
                        waiting.ClaimUrl, 
                        waiting.ExpiresAt, 
                        cancellationToken);
                }
                break;

            default:
                throw new NotSupportedException($"Outbox message type '{message.Type}' is not supported by the email service dispatcher.");
        }
    }

    #region Outbox Payload Helper Types

    public class WelcomePayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
    }

    public class VerificationPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string VerificationLink { get; set; } = string.Empty;
    }

    public class PasswordResetPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string ResetLink { get; set; } = string.Empty;
    }

    public class SecurityNotificationPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string SecurityAction { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public class TicketConfirmationPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string BookingRef { get; set; } = string.Empty;
        public string PdfAttachmentBase64 { get; set; } = string.Empty;
    }

    public class EventReminderPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int HoursRemaining { get; set; }
    }

    public class EventUpdatedPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public string UpdatedDetails { get; set; } = string.Empty;
        public string ChangeLog { get; set; } = string.Empty;
    }

    public class EventCancelledPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RefundConfirmationPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string BookingRef { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime RefundDate { get; set; }
    }

    public class WeeklyDigestPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public List<WeeklyDigestEventItem> Events { get; set; } = [];
    }

    public class WeeklyDigestEventItem
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class OrganizerEmailPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
    }

    public class OrganizerEmailWithReasonPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class OrganizerEmailSoldOutPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
    }

    public class PostEventFeedbackPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string FeedbackUrl { get; set; } = string.Empty;
    }

    public class WaitingListNotificationPayload
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public string TicketTypeName { get; set; } = string.Empty;
        public string ClaimUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    #endregion
}
