
namespace EventifyPro.Web.Controllers;

public class TestEmailsController : Controller
{
    private readonly IEmailService _emailService;
    private readonly IOutboxService _outboxService;

    public TestEmailsController(IEmailService emailService, IOutboxService outboxService)
    {
        _emailService = emailService;
        _outboxService = outboxService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var sentEmails = GetSentEmailsList();
        var html = BuildDashboardHtml(sentEmails, "");
        return Content(html, "text/html");
    }

    [HttpPost]
    public async Task<IActionResult> TriggerDirect()
    {
        try
        {
            var email = "test-attendee@example.com";
            var name = "Muhammed Mahmoud";

            // 1. Welcome Email
            await _emailService.SendWelcomeEmailAsync(email, name);

            // 2. Weekly Digest
            var mockEvents = new List<(string Title, string Category, DateTime StartDate, string Location, string Url)>
            {
                ("AI Summit 2026", "Technology", DateTime.UtcNow.AddDays(4), "Cairo Conference Center", "https://localhost:7198/events/1"),
                ("Coldplay Tribute Concert", "Music", DateTime.UtcNow.AddDays(6), "Alexandria Stadium", "https://localhost:7198/events/2"),
                ("Startup Pitch Night", "Business", DateTime.UtcNow.AddDays(9), "Grillon Space, Downtown", "https://localhost:7198/events/3")
            };
            await _emailService.SendWeeklyDigestEmailAsync(email, name, mockEvents);

            // 3. Ticket Confirmation
            var dummyPdfBytes = "Dummy PDF content for tickets"u8.ToArray();
            await _emailService.SendBookingConfirmationAsync(email, name, "BK-98317-EP", dummyPdfBytes);

            // 4. Event Reminders (7d, 24h, 2h)
            await _emailService.SendEventReminderEmailAsync(email, name, "AI Summit 2026", DateTime.UtcNow.AddDays(7), "Cairo Conference Center", 7 * 24);
            await _emailService.SendEventReminderEmailAsync(email, name, "Coldplay Tribute Concert", DateTime.UtcNow.AddDays(1), "Alexandria Stadium", 24);
            await _emailService.SendEventReminderEmailAsync(email, name, "Startup Pitch Night", DateTime.UtcNow.AddHours(2), "Grillon Space, Downtown", 2);

            // 5. Event Updated
            await _emailService.SendEventUpdatedEmailAsync(
                email, 
                name, 
                "AI Summit 2026", 
                "<p><strong>Date:</strong> 15 June 2026<br/><strong>Time:</strong> 09:00 AM<br/><strong>Location:</strong> Hall 4, Cairo Conference Center (Moved from Hall 1)</p>", 
                "Moved to Hall 4 for larger capacity. Starting time adjusted to 9:00 AM.");

            // 6. Event Cancelled
            await _emailService.SendCancellationAsync(email, name, "Beach Music Festival", "Extreme weather conditions forecasted.");

            // 7. Refund Confirmation
            await _emailService.SendRefundConfirmationAsync(email, name, "BK-98317-EP", 450.00m, DateTime.UtcNow);

            // 8. Password Reset
            await _emailService.SendPasswordResetAsync(email, name, "R1P2T3");

            // 9. Email Verification
            await _emailService.SendEmailVerificationAsync(email, name, "E7V8P9");

            // 10. Account Security
            await _emailService.SendSecurityNotificationAsync(email, name, "Password Changed", "Your password was successfully updated from a Chrome browser on Windows 11.");

            // 11. Organizer Emails
            var orgEmail = "organizer-pro@example.com";
            var orgName = "Samy Organizers Inc.";
            await _emailService.SendOrganizerEventApprovedAsync(orgEmail, orgName, "Mega Tech Exhibition");
            await _emailService.SendOrganizerEventRejectedAsync(orgEmail, orgName, "Secret Underground Party", "Events must comply with local safety licenses. Please upload permits.");
            await _emailService.SendOrganizerEventPublishedAsync(orgEmail, orgName, "Mega Tech Exhibition");
            await _emailService.SendOrganizerEventSoldOutAsync(orgEmail, orgName, "Mega Tech Exhibition", 500);

            // 12. Post Event Feedback
            await _emailService.SendPostEventFeedbackAsync(email, name, "AI Summit 2026", 1, "https://localhost:7198/feedback/event/1");

            // 13. Waiting List
            await _emailService.SendWaitingListNotificationAsync(email, name, "Sold Out Rock Concert", "VIP Front Row", "https://localhost:7198/waitinglist/claim/99", DateTime.UtcNow.AddHours(2));

            // 14. Scanner Credentials
            await _emailService.SendScannerCredentialsEmailAsync(email, name, "TempPass123!", "Event Organizer Ltd.");

            var sentEmails = GetSentEmailsList();
            var html = BuildDashboardHtml(sentEmails, "<div class='alert alert-success'>Successfully triggered all 18 test scenarios directly! Check files below.</div>");
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            var sentEmails = GetSentEmailsList();
            var html = BuildDashboardHtml(sentEmails, $"<div class='alert alert-danger'>Error: {ex.Message}</div>");
            return Content(html, "text/html");
        }
    }

    [HttpPost]
    public async Task<IActionResult> TriggerOutbox()
    {
        try
        {
            var email = "outbox-attendee@example.com";
            var name = "Mahmoud Gouda";

            // Enqueue all into the outbox
            await _outboxService.EnqueueAsync("Email.Welcome", new OutboxService.WelcomePayload
            {
                RecipientEmail = email,
                RecipientName = name
            });

            await _outboxService.EnqueueAsync("Email.Verification", new OutboxService.VerificationPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                OtpCode = "A1B2C3"
            });

            await _outboxService.EnqueueAsync("Email.PasswordReset", new OutboxService.PasswordResetPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                OtpCode = "P5W6D7"
            });

            await _outboxService.EnqueueAsync("Email.SecurityNotification", new OutboxService.SecurityNotificationPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                SecurityAction = "New Login Device Detected",
                Details = "IP Address 197.34.82.11 - Safari on macOS"
            });

            var dummyPdfBytes = "Dummy Outbox PDF content"u8.ToArray();
            await _outboxService.EnqueueAsync("Email.TicketConfirmation", new OutboxService.TicketConfirmationPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                BookingRef = "BK-OUTBOX-99",
                PdfAttachmentBase64 = Convert.ToBase64String(dummyPdfBytes)
            });

            await _outboxService.EnqueueAsync("Email.EventReminder", new OutboxService.EventReminderPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                EventTitle = "Global Fintech Gala",
                StartDate = DateTime.UtcNow.AddHours(24),
                Location = "Grand Nile Tower, Cairo",
                HoursRemaining = 24
            });

            await _outboxService.EnqueueAsync("Email.EventUpdated", new OutboxService.EventUpdatedPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                EventTitle = "Global Fintech Gala",
                UpdatedDetails = "<p><strong>Location:</strong> Ballroom A (Moved from Ballroom B)</p>",
                ChangeLog = "Changed ballroom inside the hotel."
            });

            await _outboxService.EnqueueAsync("Email.EventCancelled", new OutboxService.EventCancelledPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                EventTitle = "Canceled Concert",
                Reason = "Artist fell ill."
            });

            await _outboxService.EnqueueAsync("Email.RefundConfirmation", new OutboxService.RefundConfirmationPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                BookingRef = "BK-OUTBOX-99",
                Amount = 1500.00m,
                RefundDate = DateTime.UtcNow
            });

            await _outboxService.EnqueueAsync("Email.WeeklyDigest", new OutboxService.WeeklyDigestPayload
            {
                RecipientEmail = email,
                RecipientName = name,
                Events = new List<OutboxService.WeeklyDigestEventItem>
                {
                    new() { Title = "AI Startup Day", Category = "Technology", StartDate = DateTime.UtcNow.AddDays(3), Location = "AUC Campus", Url = "https://localhost:7198/events/4" },
                    new() { Title = "Cairo Jazz Festival", Category = "Music", StartDate = DateTime.UtcNow.AddDays(5), Location = "Al-Azhar Park", Url = "https://localhost:7198/events/5" }
                }
            });

            // Trigger Outbox Processing Immediately
            await _outboxService.ProcessPendingAsync();

            var sentEmails = GetSentEmailsList();
            var html = BuildDashboardHtml(sentEmails, "<div class='alert alert-success'>Successfully enqueued and processed all items using the Outbox Service! Check files below.</div>");
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            var sentEmails = GetSentEmailsList();
            var html = BuildDashboardHtml(sentEmails, $"<div class='alert alert-danger'>Outbox Processing Error: {ex.Message}</div>");
            return Content(html, "text/html");
        }
    }

    [HttpPost]
    public IActionResult ClearLogs()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "sent_emails");
            if (!Directory.Exists(path) && AppContext.BaseDirectory.Contains("bin"))
            {
                var projDir = AppContext.BaseDirectory;
                while (projDir != null && !Directory.Exists(Path.Combine(projDir, "EventifyPro.Web")))
                {
                    projDir = Path.GetDirectoryName(projDir);
                }
                if (projDir != null)
                {
                    path = Path.Combine(projDir, "EventifyPro.Web", "wwwroot", "sent_emails");
                }
            }

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.html");
                foreach (var file in files)
                {
                    System.IO.File.Delete(file);
                }
            }

            var sentEmails = GetSentEmailsList();
            var html = BuildDashboardHtml(sentEmails, "<div class='alert alert-info'>Cleared all generated HTML preview logs!</div>");
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            var sentEmails = GetSentEmailsList();
            var html = BuildDashboardHtml(sentEmails, $"<div class='alert alert-danger'>Error clearing files: {ex.Message}</div>");
            return Content(html, "text/html");
        }
    }

    private List<(string Filename, DateTime CreatedAt)> GetSentEmailsList()
    {
        var list = new List<(string Filename, DateTime CreatedAt)>();
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "sent_emails");
            if (!Directory.Exists(path) && AppContext.BaseDirectory.Contains("bin"))
            {
                var projDir = AppContext.BaseDirectory;
                while (projDir != null && !Directory.Exists(Path.Combine(projDir, "EventifyPro.Web")))
                {
                    projDir = Path.GetDirectoryName(projDir);
                }
                if (projDir != null)
                {
                    path = Path.Combine(projDir, "EventifyPro.Web", "wwwroot", "sent_emails");
                }
            }

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.html");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    list.Add((fileInfo.Name, fileInfo.CreationTimeUtc));
                }
            }
        }
        catch
        {
            // Ignore
        }
        return list.OrderByDescending(f => f.CreatedAt).ToList();
    }

    private string BuildDashboardHtml(List<(string Filename, DateTime CreatedAt)> files, string alertMessage)
    {
        var filesListHtml = new System.Text.StringBuilder();
        if (files == null || files.Count == 0)
        {
            filesListHtml.Append("<p style='color:#6b7280; text-align:center;'>No preview files generated yet. Use the trigger buttons above!</p>");
        }
        else
        {
            foreach (var f in files)
            {
                filesListHtml.Append($@"
                    <div style='background:#ffffff; border:1px solid #e5e7eb; border-radius:10px; padding:16px; margin-bottom:12px; display:flex; justify-content:space-between; align-items:center; transition: all 0.2s;'>
                        <div>
                            <span style='font-size:12px; color:#a1a1aa; font-weight:600;'>{f.CreatedAt.ToEgyptTime():yyyy-MM-dd HH:mm:ss}</span>
                            <div style='font-weight:700; color:#1e1b4b; font-size:15px; margin-top:4px;'>{f.Filename.Replace(".html", "").Replace("_", " ")}</div>
                        </div>
                        <a href='/sent_emails/{f.Filename}' target='_blank' style='background:#4f46e5; color:#ffffff; padding:8px 16px; border-radius:6px; font-weight:600; text-decoration:none; font-size:13px; box-shadow: 0 4px 6px -1px rgba(79,70,229,0.2);'>Open Preview ↗</a>
                    </div>
                ");
            }
        }

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>EventifyPro Email Testing Dashboard</title>
    <style>
        body {{
            font-family: 'Outfit', 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #eef2ff 0%, #f5f3ff 100%);
            min-height: 100vh;
            margin: 0;
            padding: 40px 20px;
            color: #1e1b4b;
        }}
        .container {{
            max-width: 900px;
            margin: 0 auto;
            background: rgba(255, 255, 255, 0.7);
            backdrop-filter: blur(16px);
            border: 1px solid rgba(255, 255, 255, 0.5);
            border-radius: 24px;
            box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.05), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
            padding: 40px;
        }}
        h1 {{
            font-size: 32px;
            font-weight: 800;
            background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            margin-top: 0;
            margin-bottom: 8px;
            text-align: center;
        }}
        .subtitle {{
            text-align: center;
            color: #6b7280;
            margin-bottom: 40px;
            font-size: 16px;
        }}
        .actions {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 40px;
        }}
        @media(max-width: 600px) {{
            .actions {{
                grid-template-columns: 1fr;
            }}
        }}
        .btn {{
            border: none;
            padding: 16px 24px;
            border-radius: 12px;
            font-weight: 700;
            font-size: 15px;
            cursor: pointer;
            transition: all 0.2s;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
            text-align: center;
            display: block;
            width: 100%;
        }}
        .btn-primary {{
            background: linear-gradient(135deg, #4f46e5 0%, #6366f1 100%);
            color: white;
        }}
        .btn-primary:hover {{
            opacity: 0.95;
            transform: translateY(-2px);
            box-shadow: 0 8px 12px -1px rgba(79, 70, 229, 0.3);
        }}
        .btn-secondary {{
            background: linear-gradient(135deg, #7c3aed 0%, #8b5cf6 100%);
            color: white;
        }}
        .btn-secondary:hover {{
            opacity: 0.95;
            transform: translateY(-2px);
            box-shadow: 0 8px 12px -1px rgba(124, 58, 237, 0.3);
        }}
        .btn-clear {{
            background: transparent;
            border: 1px dashed #ef4444;
            color: #ef4444;
            margin: 0 auto;
            max-width: 200px;
            padding: 10px 16px;
            font-size: 13px;
        }}
        .btn-clear:hover {{
            background: #fee2e2;
        }}
        .alert {{
            padding: 16px;
            border-radius: 12px;
            margin-bottom: 30px;
            font-weight: 600;
            text-align: center;
            font-size: 14px;
        }}
        .alert-success {{
            background: #d1fae5;
            color: #065f46;
            border: 1px solid #a7f3d0;
        }}
        .alert-danger {{
            background: #fee2e2;
            color: #991b1b;
            border: 1px solid #fca5a5;
        }}
        .alert-info {{
            background: #e0f2fe;
            color: #0369a1;
            border: 1px solid #bae6fd;
        }}
        .logs-section {{
            background: rgba(255, 255, 255, 0.9);
            border: 1px solid #e5e7eb;
            border-radius: 16px;
            padding: 24px;
        }}
        .logs-title {{
            font-size: 18px;
            font-weight: 700;
            color: #1e1b4b;
            margin-top: 0;
            margin-bottom: 20px;
            border-bottom: 2px solid #e5e7eb;
            padding-bottom: 10px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>⚡ EventifyPro</h1>
        <div class=""subtitle"">Email Delivery & Transactional Outbox Testing Dashboard</div>
        
        {alertMessage}

        <div class=""actions"">
            <form action=""/TestEmails/TriggerDirect"" method=""POST"">
                <button type=""submit"" class=""btn btn-primary"">🎯 Trigger All Scenario Emails Directly</button>
            </form>
            <form action=""/TestEmails/TriggerOutbox"" method=""POST"">
                <button type=""submit"" class=""btn btn-secondary"">📦 Trigger All Emails via Outbox Pipeline</button>
            </form>
        </div>

        <div class=""logs-section"">
            <div class=""logs-title"">
                <span>📁 Generated Email Preview Logs</span>
                <form action=""/TestEmails/ClearLogs"" method=""POST"" style=""margin:0;"">
                    <button type=""submit"" class=""btn btn-clear"">🗑️ Clear Logs</button>
                </form>
            </div>
            <div class=""logs-list"">
                {filesListHtml}
            </div>
        </div>
    </div>
</body>
</html>";
    }
}
