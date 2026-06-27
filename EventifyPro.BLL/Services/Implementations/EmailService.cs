namespace EventifyPro.BLL.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    #region Generic Send Methods

    public async Task SendEmailAsync(
        string to, 
        string subject, 
        string htmlBody, 
        byte[]? attachment = null, 
        string? attachmentName = null, 
        CancellationToken cancellationToken = default)
    {
        var settings = _configuration.GetSection("EmailSettings");
        var host = settings["Host"] ?? "localhost";
        var port = int.TryParse(settings["Port"], out var p) ? p : 25;
        var username = settings["Username"] ?? "";
        var password = settings["Password"] ?? "";
        var enableSsl = bool.TryParse(settings["EnableSsl"], out var ssl) && ssl;
        var senderEmail = settings["SenderEmail"] ?? "no-reply@eventifypro.com";
        var senderName = settings["SenderName"] ?? "EventifyPro Support";
        var saveLocally = bool.TryParse(settings["SaveLocallyForTesting"], out var local) && local;

        var fullHtml = BuildLayout(subject, htmlBody);

        // 1. Visual Verification (Save to wwwroot/sent_emails/ for testing/preview)
        if (saveLocally)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                // Traverse up to find EventifyPro.Web directory or output inside base path
                var outPath = Path.Combine(baseDir, "wwwroot", "sent_emails");
                
                // Fallback attempt to search for solution folders if baseDir is under bin
                if (!Directory.Exists(outPath) && baseDir.Contains("bin"))
                {
                    var projDir = baseDir;
                    while (projDir != null && !Directory.Exists(Path.Combine(projDir, "EventifyPro.Web")))
                    {
                        projDir = Path.GetDirectoryName(projDir);
                    }
                    if (projDir != null)
                    {
                        outPath = Path.Combine(projDir, "EventifyPro.Web", "wwwroot", "sent_emails");
                    }
                }

                Directory.CreateDirectory(outPath);
                
                var sanitizedSubject = string.Concat(subject.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                var filename = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{to}_{sanitizedSubject}.html";
                var filePath = Path.Combine(outPath, filename);
                
                await File.WriteAllTextAsync(filePath, fullHtml, Encoding.UTF8, cancellationToken);
                Console.WriteLine($"[EmailService] Visual preview saved to: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Failed to write email preview: {ex.Message}");
            }
        }

        // 2. Real SMTP Delivery
        try
        {
            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(senderEmail, senderName);
            mailMessage.To.Add(new MailAddress(to));
            mailMessage.Subject = subject;
            mailMessage.Body = fullHtml;
            mailMessage.IsBodyHtml = true;

            if (attachment != null && !string.IsNullOrEmpty(attachmentName))
            {
                var ms = new MemoryStream(attachment);
                mailMessage.Attachments.Add(new Attachment(ms, attachmentName));
            }

            using var smtpClient = new SmtpClient(host, port);
            smtpClient.EnableSsl = enableSsl;
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                smtpClient.Credentials = new NetworkCredential(username, password);
            }

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            // If SMTP fails, we log it, but do not crash the application if we saved it locally
            Console.WriteLine($"[EmailService] SMTP delivery to {to} failed: {ex.Message}");
            if (!saveLocally)
            {
                throw;
            }
        }
    }

    private string BuildLayout(string subject, string bodyContent)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "https://eventifypro.runasp.net";
        
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{subject}</title>
    <style>
        body {{
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
            background-color: #fafaff;
            margin: 0;
            padding: 0;
            color: #1f1b2e;
            -webkit-font-smoothing: antialiased;
        }}
        .wrapper {{
            width: 100%;
            background-color: #fafaff;
            padding: 40px 0;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 20px;
            box-shadow: 0 10px 30px rgba(91, 33, 182, 0.08);
            overflow: hidden;
            border: 1px solid rgba(218, 220, 224, 0.7);
        }}
        .header {{
            background: linear-gradient(135deg, #5b21b6 0%, #7c3aed 100%);
            padding: 42px 32px;
            text-align: center;
            color: #ffffff;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 800;
            letter-spacing: -0.03em;
            display: inline-flex;
            align-items: center;
            gap: 10px;
        }}
        .content {{
            padding: 40px 36px;
            line-height: 1.6;
        }}
        .content h2 {{
            margin-top: 0;
            font-size: 22px;
            color: #1f1b2e;
            font-weight: 800;
            letter-spacing: -0.02em;
        }}
        .content p {{
            color: #6b7280;
            margin: 16px 0;
            font-size: 15px;
        }}
        .button-wrapper {{
            margin: 36px 0;
            text-align: center;
        }}
        .button {{
            display: inline-block;
            background: linear-gradient(135deg, #5b21b6 0%, #7c3aed 100%);
            color: #ffffff !important;
            padding: 14px 32px;
            border-radius: 50px;
            font-weight: 700;
            text-decoration: none;
            box-shadow: 0 8px 24px rgba(124, 58, 237, 0.3);
            font-size: 15px;
        }}
        .button:hover {{
            box-shadow: 0 12px 30px rgba(124, 58, 237, 0.4);
        }}
        .details-card {{
            background-color: #f5f3ff;
            border: 1px solid #ede9fe;
            border-radius: 16px;
            padding: 24px;
            margin: 28px 0;
        }}
        .details-row {{
            margin-bottom: 14px;
            font-size: 14px;
            border-bottom: 1px solid #ede9fe;
            padding-bottom: 10px;
        }}
        .details-row:last-child {{
            margin-bottom: 0;
            border-bottom: none;
            padding-bottom: 0;
        }}
        .details-label {{
            color: #6b7280;
            font-weight: 700;
            display: inline-block;
            width: 140px;
        }}
        .details-value {{
            color: #1f1b2e;
            font-weight: 600;
        }}
        .divider {{
            height: 1px;
            background-color: #ede9fe;
            margin: 28px 0;
        }}
        .badge {{
            display: inline-block;
            padding: 4px 12px;
            border-radius: 999px;
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }}
        .badge-success {{ background-color: #d1fae5; color: #065f46; }}
        .badge-warning {{ background-color: #fef3c7; color: #92400e; }}
        .badge-danger {{ background-color: #fee2e2; color: #991b1b; }}
        .badge-info {{ background-color: #e0f2fe; color: #075985; }}
        
        .footer {{
            background-color: #f5f3ff;
            padding: 32px;
            text-align: center;
            font-size: 13px;
            color: #6b7280;
            border-top: 1px solid #ede9fe;
        }}
        .footer a {{
            color: #5b21b6;
            text-decoration: none;
            font-weight: 600;
        }}
        .qr-code {{
            text-align: center;
            margin: 24px 0;
        }}
        .qr-code img {{
            border: 4px solid #ffffff;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
            border-radius: 8px;
            width: 180px;
            height: 180px;
        }}
        .event-item {{
            margin-bottom: 20px;
            padding-bottom: 16px;
            border-bottom: 1px solid #e5e7eb;
        }}
        .event-item:last-child {{
            margin-bottom: 0;
            padding-bottom: 0;
            border-bottom: none;
        }}
        .event-title {{
            font-weight: 700;
            color: #111827;
            font-size: 16px;
        }}
        .event-meta {{
            font-size: 13px;
            color: #6b7280;
            margin-top: 4px;
        }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <div class=""header"">
                <h1 style=""display: inline-flex; align-items: center; justify-content: center; gap: 10px; margin: 0; font-size: 28px; font-weight: 800; color: #ffffff;"">
                    <span style=""background: #ffffff; padding: 4px; border-radius: 8px; display: inline-flex; align-items: center; justify-content: center; width: 36px; height: 36px;"">
                        <img src=""{baseUrl}/Images/Logo.png"" alt="""" style=""width: 100%; height: 100%; object-fit: contain; border-radius: 4px;"" />
                    </span>
                    <span>Eventify Pro</span>
                </h1>
            </div>
            <div class=""content"">
                {bodyContent}
            </div>
            <div class=""footer"">
                <p>You received this email because you are registered on Eventify Pro.</p>
                <p>&copy; 2026 Eventify Pro Inc. All rights reserved.</p>
                <p>
                    <a href=""{baseUrl}/Home/Privacy"">Privacy Policy</a> &bull; 
                    <a href=""{baseUrl}/Home/Terms"">Terms of Service</a> &bull; 
                    <a href=""{baseUrl}/Home/HelpCenter"">Help Center</a>
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    #endregion

    #region Scenario 1: Welcome Email
    public async Task SendWelcomeEmailAsync(string recipientEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "https://eventifypro.runasp.net";
        var subject = "Welcome to EventifyPro! 🎉";
        var body = $@"
            <h2>Welcome aboard, {recipientName}!</h2>
            <p>We are thrilled to have you join EventifyPro—the ultimate platform to organize, discover, and attend amazing events.</p>
            <p>Here is what you can do next:</p>
            <ul>
                <li><strong>Explore:</strong> Search through hundreds of live concerts, conferences, and technical workshops.</li>
                <li><strong>Tickets:</strong> Get instantly-generated PDF tickets and secure QR scanning tokens.</li>
                <li><strong>Organize:</strong> Host your own events, set ticket pricing, and track live attendee check-ins.</li>
            </ul>
            <p>Get started by setting up your dashboard profile right now.</p>
            <div class=""button-wrapper"">
                <a href=""{baseUrl}/Account/Login"" class=""button"">Go to Dashboard</a>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 2: Weekly Digest Email
    public async Task SendWeeklyDigestEmailAsync(
        string recipientEmail, 
        string recipientName, 
        List<(string Title, string Category, DateTime StartDate, string Location, string Url)> events, 
        CancellationToken cancellationToken = default)
    {
        var subject = "Weekly Hot Events on EventifyPro! 🔥";
        var eventsListHtml = new StringBuilder();

        if (events == null || events.Count == 0)
        {
            eventsListHtml.Append("<p>No new events this week, but check back soon as organizers add details daily!</p>");
        }
        else
        {
            foreach (var evt in events)
            {
                eventsListHtml.Append($@"
                    <div class=""event-item"">
                        <div class=""event-title"">{evt.Title} <span class=""badge badge-info"">{evt.Category}</span></div>
                        <div class=""event-meta"">
                            📅 {evt.StartDate.ToEgyptTime():dd MMM yyyy, hh:mm tt} | 📍 {evt.Location}
                        </div>
                        <p style=""margin: 6px 0;""><a href=""{evt.Url}"" style=""color: #4f46e5; font-weight:600;"">View Details &rarr;</a></p>
                    </div>
                ");
            }
        }

        var body = $@"
            <h2>Hey {recipientName},</h2>
            <p>Here is your weekly digest of the top events on EventifyPro tailored for you:</p>
            <div class=""details-card"">
                {eventsListHtml}
            </div>
            <p>Book your spots before they sell out!</p>
            <div class=""button-wrapper"">
                <a href=""{_configuration["BaseUrl"] ?? "https://eventifypro.runasp.net"}"" class=""button"">Browse All Events</a>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 3: Ticket Confirmation Email
    public async Task SendBookingConfirmationAsync(
        string recipientEmail, 
        string recipientName, 
        string bookingRef, 
        byte[] pdfAttachment, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"Ticket Confirmation - Ref: {bookingRef} 🎫";
        var body = $@"
            <h2>Your Booking is Confirmed!</h2>
            <p>Thank you for purchasing tickets on EventifyPro. We have processed your order successfully.</p>
            <div class=""details-card"">
                <div class=""details-row"">
                    <span class=""details-label"">Booking Reference:</span>
                    <span class=""details-value""><strong>{bookingRef}</strong></span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Attendee Name:</span>
                    <span class=""details-value"">{recipientName}</span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Status:</span>
                    <span class=""badge badge-success"">Paid</span>
                </div>
            </div>
            <p>We have attached your printable PDF tickets to this email. Each ticket includes a unique secure QR code which will be scanned at the entrance.</p>
            <p>Please keep this email and the attachment handy upon arrival.</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, pdfAttachment, $"Tickets_{bookingRef}.pdf", cancellationToken);
    }
    #endregion

    #region Scenario 4: Event Reminder Email
    public async Task SendEventReminderEmailAsync(
        string recipientEmail, 
        string recipientName, 
        string eventTitle, 
        DateTime startDate, 
        string location, 
        int hoursRemaining, 
        CancellationToken cancellationToken = default)
    {
        var timeMessage = hoursRemaining switch
        {
            2 => "starts in just 2 hours! ⏰ Get ready!",
            24 => "is happening tomorrow! 🗓️ Check details below.",
            _ => $"is coming up in {hoursRemaining / 24} days!"
        };

        var subject = $"Reminder: {eventTitle} {timeMessage}";
        var body = $@"
            <h2>Friendly Reminder!</h2>
            <p>Hi {recipientName}, this is a reminder that the event you booked tickets for starts soon.</p>
            <div class=""details-card"">
                <div class=""details-row"">
                    <span class=""details-label"">Event:</span>
                    <span class=""details-value""><strong>{eventTitle}</strong></span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Starts At:</span>
                    <span class=""details-value"">{startDate.ToEgyptTime():dd MMM yyyy, hh:mm tt}</span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Location:</span>
                    <span class=""details-value"">{location}</span>
                </div>
            </div>
            <p>Make sure to have your tickets ready. You can find them in your dashboard profile or attached to your booking confirmation email.</p>
            <div class=""button-wrapper"">
                <a href=""{_configuration["BaseUrl"] ?? "https://eventifypro.runasp.net"}/Account/Login"" class=""button"">View My Tickets</a>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 5: Event Updated Email
    public async Task SendEventUpdatedEmailAsync(
        string recipientEmail, 
        string recipientName, 
        string eventTitle, 
        string updatedDetails, 
        string changeLog, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"Important: Event Updated - {eventTitle} ⚠️";
        var body = $@"
            <h2>Event Details Updated</h2>
            <p>Hi {recipientName},</p>
            <p>The organizer of <strong>{eventTitle}</strong> has updated some details of the event you are registered for.</p>
            <div class=""details-card"">
                <h3 style=""margin-top: 0; font-size:15px; color:#ef4444;"">Summary of Changes:</h3>
                <p style=""background: #fee2e2; padding: 12px; border-radius: 6px; font-family: monospace; font-size: 13px; color: #991b1b; margin: 8px 0;"">
                    {changeLog}
                </p>
                <div class=""divider""></div>
                <h3 style=""font-size:15px;"">Updated Event Details:</h3>
                {updatedDetails}
            </div>
            <p>Please review these changes. If you can no longer attend, please access your bookings on the platform to check refund settings.</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 6: Event Cancelled Email
    public async Task SendCancellationAsync(
        string recipientEmail, 
        string recipientName, 
        string eventTitle, 
        string reason, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"Alert: Event Cancelled - {eventTitle} ❌";
        var body = $@"
            <h2>Event Cancelled</h2>
            <p>Dear {recipientName},</p>
            <p>We regret to inform you that the event <strong>{eventTitle}</strong> has been cancelled by the organizer.</p>
            <div class=""details-card"">
                <div class=""details-row"">
                    <span class=""details-label"">Reason:</span>
                    <span class=""details-value"" style=""color:#b91c1c;""><strong>{reason}</strong></span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Refund Status:</span>
                    <span class=""badge badge-danger"">Cancelled</span>
                </div>
            </div>
            <h3>Refund Policy:</h3>
            <p>Since the event was cancelled, a full refund will be processed automatically back to your payment method. Please allow 5-10 business days for the transaction to reflect in your account.</p>
            <p>If you have any questions, you can contact our support center.</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 7: Refund Confirmation Email
    public async Task SendRefundConfirmationAsync(
        string recipientEmail, 
        string recipientName, 
        string bookingRef, 
        decimal amount, 
        DateTime refundDate, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"Refund Processed: Ref {bookingRef} 💸";
        var body = $@"
            <h2>Refund Confirmation</h2>
            <p>Hi {recipientName},</p>
            <p>Your refund request for booking reference <strong>{bookingRef}</strong> has been successfully processed.</p>
            <div class=""details-card"">
                <div class=""details-row"">
                    <span class=""details-label"">Refund Amount:</span>
                    <span class=""details-value"" style=""color:#047857;""><strong>{amount:C}</strong></span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Refund Date:</span>
                    <span class=""details-value"">{refundDate.ToEgyptTime():dd MMM yyyy, hh:mm tt}</span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Status:</span>
                    <span class=""badge badge-success"">Refunded</span>
                </div>
            </div>
            <p>Funds have been returned to your original payment card. Depending on your bank, it may take a few business days for the statement credit to appear.</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 8: Password Reset Email
    public async Task SendPasswordResetAsync(
        string recipientEmail, 
        string recipientName, 
        string otpCode, 
        CancellationToken cancellationToken = default)
    {
        var subject = "Reset Your Password - Eventify Pro 🔑";
        var body = $@"
            <h2>Reset Your Password</h2>
            <p>Hi <strong>{recipientName}</strong>,</p>
            <p>We received a request to reset the password for your Eventify Pro account. Please use the 6-character verification code below to reset your password:</p>
            <div style=""text-align: center; margin: 36px 0;"">
                <span style=""font-size: 32px; font-weight: 800; letter-spacing: 6px; color: #ffffff; background: linear-gradient(135deg, #5b21b6 0%, #7c3aed 100%); padding: 14px 28px; border-radius: 12px; box-shadow: 0 8px 24px rgba(124, 58, 237, 0.3); display: inline-block;"">
                    {otpCode}
                </span>
            </div>
            <div class=""details-card"">
                <p style=""margin: 0; font-size: 13px; color: #6b7280;"">
                    ⚠️ <strong>Security Notice:</strong> This verification code is valid for 15 minutes. If you did not request a password reset, please ignore this email or contact support if you suspect unauthorized access.
                </p>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 9: Email Verification
    public async Task SendEmailVerificationAsync(
        string recipientEmail, 
        string recipientName, 
        string otpCode, 
        CancellationToken cancellationToken = default)
    {
        var subject = "Verify Your Email Address - Eventify Pro ✉️";
        var body = $@"
            <h2>Welcome to Eventify Pro! 🎉</h2>
            <p>Hi <strong>{recipientName}</strong>,</p>
            <p>Thank you for signing up! To fully activate and secure your account, please confirm your email address by entering the 6-character verification code below on the confirmation page:</p>
            <div style=""text-align: center; margin: 36px 0;"">
                <span style=""font-size: 32px; font-weight: 800; letter-spacing: 6px; color: #ffffff; background: linear-gradient(135deg, #5b21b6 0%, #7c3aed 100%); padding: 14px 28px; border-radius: 12px; box-shadow: 0 8px 24px rgba(124, 58, 237, 0.3); display: inline-block;"">
                    {otpCode}
                </span>
            </div>
            <div class=""details-card"">
                <p style=""margin: 0; font-size: 13px; color: #6b7280;"">
                    This verification code is valid for 15 minutes. Confirming your email ensures you will receive your ticket purchase confirmations, PDF tickets, and QR code access passes directly to your inbox.
                </p>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 10: Account Security Email
    public async Task SendSecurityNotificationAsync(
        string recipientEmail, 
        string recipientName, 
        string securityAction, 
        string details, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"Security Alert: Account {securityAction} 🛡️";
        var body = $@"
            <h2>Security Notification</h2>
            <p>Hi {recipientName},</p>
            <p>This is a security alert regarding your EventifyPro account. A critical security settings change was made:</p>
            <div class=""details-card"">
                <div class=""details-row"">
                    <span class=""details-label"">Action:</span>
                    <span class=""details-value"" style=""color:#b91c1c; font-weight:700;"">{securityAction}</span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Date:</span>
                    <span class=""details-value"">{DateTime.UtcNow:dd MMM yyyy, hh:mm tt} (UTC)</span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Details:</span>
                    <span class=""details-value"">{details}</span>
                </div>
            </div>
            <p style=""color:#b91c1c; font-weight:600;"">If you did NOT perform this action, please reset your password immediately and contact support to lock your account.</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 11: Organizer Emails

    public async Task SendOrganizerAccountActivatedAsync(string recipientEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "https://eventifypro.runasp.net";
        var subject = "Your Organizer Account has been Activated! 🚀";
        var body = $@"
            <h2>Congratulations, {recipientName}!</h2>
            <p>We are excited to let you know that our administration team has verified your business details and successfully activated your Organizer Account on EventifyPro.</p>
            <p>You can now:</p>
            <ul>
                <li>Create and publish public or private events.</li>
                <li>Design custom ticket types (Free, Paid, VIP) and set prices.</li>
                <li>Access the Organizer Dashboard to track sales, view statistics, and scan tickets at the gate.</li>
            </ul>
            <p>Let's get started by creating your very first event!</p>
            <div class=""button-wrapper"">
                <a href=""{baseUrl}/Organizer"" class=""button"">Go to Organizer Dashboard</a>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }

    public async Task SendOrganizerEventApprovedAsync(string recipientEmail, string recipientName, string eventTitle, CancellationToken cancellationToken = default)
    {
        var subject = $"Event Approved: {eventTitle} ✅";
        var body = $@"
            <h2>Congratulations!</h2>
            <p>Hi {recipientName},</p>
            <p>Your event <strong>{eventTitle}</strong> has been successfully reviewed and approved by our moderation team.</p>
            <p>It is now live on our platform and users can browse and buy tickets!</p>
            <div class=""button-wrapper"">
                <a href=""{_configuration["BaseUrl"] ?? "https://eventifypro.runasp.net"}/Account/Login"" class=""button"">Go to Management Dashboard</a>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }

    public async Task SendOrganizerEventRejectedAsync(string recipientEmail, string recipientName, string eventTitle, string reason, CancellationToken cancellationToken = default)
    {
        var subject = $"Event Update Request: {eventTitle} ❌";
        var body = $@"
            <h2>Event Rejected</h2>
            <p>Hi {recipientName},</p>
            <p>We reviewed your event proposal for <strong>{eventTitle}</strong>, but unfortunately, it could not be approved in its current state.</p>
            <div class=""details-card"">
                <h3 style=""margin-top: 0; font-size:15px; color:#ef4444;"">Moderator Feedback:</h3>
                <p>{reason}</p>
            </div>
            <p>Please edit the event details according to our content guidelines and submit it again for verification.</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }

    public async Task SendOrganizerEventPublishedAsync(string recipientEmail, string recipientName, string eventTitle, CancellationToken cancellationToken = default)
    {
        var subject = $"Event Published: {eventTitle} 📢";
        var body = $@"
            <h2>Your Event is Live!</h2>
            <p>Hi {recipientName},</p>
            <p>We've published <strong>{eventTitle}</strong>. It is now visible to all visitors on the search feeds.</p>
            <p>You can start promoting your event link on social media to drive ticket sales.</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }

    public async Task SendOrganizerEventSoldOutAsync(string recipientEmail, string recipientName, string eventTitle, int totalTickets, CancellationToken cancellationToken = default)
    {
        var subject = $"Congratulations! Event Sold Out - {eventTitle} 🏆";
        var body = $@"
            <h2>Event Sold Out!</h2>
            <p>Awesome job, {recipientName}!</p>
            <p>Your event <strong>{eventTitle}</strong> is completely sold out! All <strong>{totalTickets}</strong> available tickets have been purchased.</p>
            <p>You can view attendee lists and download scanners logs on your dashboard.</p>
            <div class=""button-wrapper"">
                <a href=""{_configuration["BaseUrl"] ?? "https://eventifypro.runasp.net"}/Account/Login"" class=""button"">Manage Attendees</a>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }

    #endregion

    #region Scanner Credentials Email
    public async Task SendScannerCredentialsEmailAsync(
        string recipientEmail, 
        string recipientName, 
        string temporaryPassword, 
        string organizerName, 
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "https://eventifypro.runasp.net";
        var subject = "Your Scanner Credentials for EventifyPro 📱";
        var body = $@"
            <h2>Scanner Access Credentials</h2>
            <p>Hi <strong>{recipientName}</strong>,</p>
            <p>You have been assigned as a scanner by <strong>{organizerName}</strong> for their events on EventifyPro. Here are your temporary login credentials:</p>
            <div class=""details-card"">
                <div class=""details-row"">
                    <span class=""details-label"">Email:</span>
                    <span class=""details-value"">{recipientEmail}</span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Temporary Password:</span>
                    <span class=""details-value"" style=""font-family: monospace; background: #f3f4f6; padding: 8px 12px; border-radius: 6px;""><strong>{temporaryPassword}</strong></span>
                </div>
            </div>
            <p>For security, please <strong>change your password immediately</strong> after your first login.</p>
            <div class=""button-wrapper"">
                <a href=""{baseUrl}/Scanner/Login"" class=""button"">Login to Scanner App</a>
            </div>
            <div class=""details-card"">
                <p style=""margin: 0; font-size: 13px; color: #6b7280;"">
                    ⚠️ <strong>Security Notice:</strong> This temporary password will expire after your first login. Keep this email secure and do not share these credentials with anyone.
                </p>
            </div>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Payout Status Email
    public async Task SendPayoutStatusEmailAsync(
        string recipientEmail, 
        string recipientName, 
        decimal amount, 
        string status, 
        string? referenceNumber, 
        string? notes, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"Payout Request Update: {status} 💰";
        var isApproved = string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);
        
        var body = $@"
            <h2>Payout Request status: {status}</h2>
            <p>Hi {recipientName},</p>
            <p>Your request to withdraw <strong>${amount:N2}</strong> has been processed.</p>
            <div class=""details-card"">
                <div class=""details-row"">
                    <span class=""details-label"">Amount:</span>
                    <span class=""details-value""><strong>${amount:N2}</strong></span>
                </div>
                <div class=""details-row"">
                    <span class=""details-label"">Status:</span>
                    <span class=""details-value""><strong style=""color: {(isApproved ? "#22c55e" : "#ef4444")};"">{status}</strong></span>
                </div>";

        if (isApproved && !string.IsNullOrEmpty(referenceNumber))
        {
            body += $@"
                <div class=""details-row"">
                    <span class=""details-label"">Reference Number:</span>
                    <span class=""details-value"">{referenceNumber}</span>
                </div>";
        }

        if (!string.IsNullOrEmpty(notes))
        {
            body += $@"
                <div class=""details-row"">
                    <span class=""details-label"">Notes / Feedback:</span>
                    <span class=""details-value"">{notes}</span>
                </div>";
        }

        body += @"
            </div>
            <p>If you have any questions, please contact our support team.</p>
        ";

        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Scenario 12: Post Event Feedback Email
    public async Task SendPostEventFeedbackAsync(
        string recipientEmail, 
        string recipientName, 
        string eventTitle, 
        int eventId, 
        string feedbackUrl, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"How was {eventTitle}? Tell us! 📝";
        var body = $@"
            <h2>Thank You for Attending!</h2>
            <p>Hi {recipientName},</p>
            <p>We hope you had a fantastic time at <strong>{eventTitle}</strong>! To help us and the organizers improve future events, we would love to hear your feedback.</p>
            <p>Please take 2 minutes to fill out the review form:</p>
            <div class=""button-wrapper"">
                <a href=""{feedbackUrl}"" class=""button"">Write a Review</a>
            </div>
            <p>Thank you for being part of our community!</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion

    #region Original waiting list compatibility
    public async Task SendWaitingListNotificationAsync(
        string recipientEmail, 
        string recipientName, 
        string eventTitle, 
        string ticketTypeName, 
        string claimUrl, 
        DateTime expiresAt, 
        CancellationToken cancellationToken = default)
    {
        var subject = $"Ticket Available on Waiting List: {eventTitle}! 🎫";
        var body = $@"
            <h2>Great news! A spot opened up!</h2>
            <p>Hi {recipientName}, a ticket of type <strong>{ticketTypeName}</strong> is now available for <strong>{eventTitle}</strong>.</p>
            <p>Since you are on the waiting list, this ticket has been reserved for you, but you must claim it before the reservation expires.</p>
            <div class=""details-card"">
                <div class=""details-row"">
                    <span class=""details-label"">Expires At:</span>
                    <span class=""details-value"" style=""color:#b91c1c;""><strong>{expiresAt:dd MMM yyyy, hh:mm tt} (local)</strong></span>
                </div>
            </div>
            <div class=""button-wrapper"">
                <a href=""{claimUrl}"" class=""button"">Claim Ticket Now</a>
            </div>
            <p style=""font-size:13px; color:#6b7280;"">If you do not claim this ticket before the deadline, it will be automatically offered to the next person on the waiting list.</p>
        ";
        await SendEmailAsync(recipientEmail, subject, body, null, null, cancellationToken);
    }
    #endregion
}
