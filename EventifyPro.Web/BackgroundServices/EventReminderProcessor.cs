namespace EventifyPro.Web.BackgroundServices;

public class EventReminderProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventReminderProcessor> _logger;

    public EventReminderProcessor(IServiceProvider serviceProvider, ILogger<EventReminderProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event Reminder Processor Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using (var scope = _serviceProvider.CreateAsyncScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<EventifyPro.DAL.Repositories.Interfaces.IUnitOfWork>();
                    var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                    
                    await ProcessRemindersForTimeframeAsync(unitOfWork, outboxService, hoursRemaining: 7 * 24, stoppingToken); // 7 Days
                    await ProcessRemindersForTimeframeAsync(unitOfWork, outboxService, hoursRemaining: 24, stoppingToken);     // 24 Hours
                    await ProcessRemindersForTimeframeAsync(unitOfWork, outboxService, hoursRemaining: 2, stoppingToken);      // 2 Hours
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scanning event reminders.");
            }

            // Run reminder scans every 15 minutes
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }

        _logger.LogInformation("Event Reminder Processor Service is stopping.");
    }

    private async Task ProcessRemindersForTimeframeAsync(
        EventifyPro.DAL.Repositories.Interfaces.IUnitOfWork unitOfWork, 
        IOutboxService outboxService, 
        int hoursRemaining, 
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        DateTime targetStart;
        DateTime targetEnd;

        if (hoursRemaining == 7 * 24)
        {
            // 7 days +/- 12 hours window
            targetStart = utcNow.AddDays(7).AddHours(-12);
            targetEnd = utcNow.AddDays(7).AddHours(12);
        }
        else if (hoursRemaining == 24)
        {
            // 24 hours +/- 4 hours window
            targetStart = utcNow.AddHours(20);
            targetEnd = utcNow.AddHours(28);
        }
        else // 2 hours
        {
            // 2 hours +/- 30 mins window
            targetStart = utcNow.AddMinutes(90);
            targetEnd = utcNow.AddMinutes(150);
        }

        // Query upcoming published events within window
        var upcomingEvents = await unitOfWork.DbContext.Events
            .Where(e => e.Status == EventStatus.Published 
                && !e.IsDeleted 
                && e.StartDate >= targetStart 
                && e.StartDate <= targetEnd)
            .Include(e => e.Bookings)
                .ThenInclude(b => b.User)
            .ToListAsync(cancellationToken);

        foreach (var evt in upcomingEvents)
        {
            // Gather all confirmed bookings for this event
            var confirmedBookings = evt.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed && b.User != null)
                .ToList();

            foreach (var booking in confirmedBookings)
            {
                var user = booking.User;
                
                // Construct a unique payload marker to check for existing reminder outbox message
                var emailMatch = $"\"RecipientEmail\":\"{user.Email}\"";
                var titleMatch = $"\"EventTitle\":\"{evt.Title}\"";
                var hoursMatch = $"\"HoursRemaining\":{hoursRemaining}";

                // Check if we've already enqueued this reminder
                var reminderAlreadyQueued = await unitOfWork.DbContext.OutboxMessages
                    .AnyAsync(m => m.Type == "Email.EventReminder" 
                        && m.Payload.Contains(emailMatch) 
                        && m.Payload.Contains(titleMatch)
                        && m.Payload.Contains(hoursMatch), cancellationToken);

                if (!reminderAlreadyQueued)
                {
                    _logger.LogInformation($"Queuing {hoursRemaining}h reminder for user '{user.Email}' on event '{evt.Title}'");
                    
                    var payload = new OutboxService.EventReminderPayload
                    {
                        RecipientEmail = user.Email!,
                        RecipientName = user.FullName,
                        EventTitle = evt.Title,
                        StartDate = evt.StartDate,
                        Location = evt.Location,
                        HoursRemaining = hoursRemaining
                    };

                    await outboxService.EnqueueAsync("Email.EventReminder", payload, cancellationToken);
                }
            }
        }
    }
}
