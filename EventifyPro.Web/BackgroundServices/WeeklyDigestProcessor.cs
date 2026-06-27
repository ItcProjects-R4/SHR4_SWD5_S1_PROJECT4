namespace EventifyPro.Web.BackgroundServices;

public class WeeklyDigestProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyDigestProcessor> _logger;

    public WeeklyDigestProcessor(IServiceProvider serviceProvider, ILogger<WeeklyDigestProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Weekly Digest Processor Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using (var scope = _serviceProvider.CreateAsyncScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<EventifyPro.DAL.Repositories.Interfaces.IUnitOfWork>();
                    var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                    
                    await ProcessWeeklyDigestsAsync(unitOfWork, outboxService, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while compiling weekly digests.");
            }

            // Check once every 12 hours (runs in background, checks 6-day window per user)
            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }

        _logger.LogInformation("Weekly Digest Processor Service is stopping.");
    }

    private async Task ProcessWeeklyDigestsAsync(
        EventifyPro.DAL.Repositories.Interfaces.IUnitOfWork unitOfWork, 
        IOutboxService outboxService, 
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        
        // 1. Fetch newly published events (published in the last 7 days, or starting in the future)
        var newEvents = await unitOfWork.DbContext.Events
            .Where(e => e.Status == EventStatus.Published && !e.IsDeleted && e.StartDate > utcNow)
            .Include(e => e.Category)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5) // Limit to top 5 hot events
            .ToListAsync(cancellationToken);

        if (newEvents.Count == 0)
        {
            _logger.LogInformation("No new upcoming events to include in the Weekly Digest. Skipping.");
            return;
        }

        // 2. Fetch all active users (attendees and organizers)
        var users = await unitOfWork.DbContext.Users
            .Where(u => u.IsActive && u.Email != null)
            .ToListAsync(cancellationToken);

        var baseUrl = _configuration["BaseUrl"] ?? "https://eventifypro.runasp.net";

        foreach (var user in users)
        {
            // Check if user already received a Weekly Digest in the last 6 days
            var emailMatch = $"\"RecipientEmail\":\"{user.Email}\"";
            var recentlyDigestSent = await unitOfWork.DbContext.OutboxMessages
                .AnyAsync(m => m.Type == "Email.WeeklyDigest" 
                    && m.Payload.Contains(emailMatch) 
                    && m.CreatedAt >= utcNow.AddDays(-6), cancellationToken);

            if (!recentlyDigestSent)
            {
                _logger.LogInformation($"Queuing Weekly Digest for user '{user.Email}'");

                var digestEvents = newEvents.Select(e => new OutboxService.WeeklyDigestEventItem
                {
                    Title = e.Title,
                    Category = e.Category?.Name ?? "General",
                    StartDate = e.StartDate,
                    Location = e.Location,
                    Url = $"{baseUrl}/Events/Details/{e.Id}" // Dynamic event detail link
                }).ToList();

                var payload = new OutboxService.WeeklyDigestPayload
                {
                    RecipientEmail = user.Email!,
                    RecipientName = user.FullName,
                    Events = digestEvents
                };

                await outboxService.EnqueueAsync("Email.WeeklyDigest", payload, cancellationToken);
            }
        }
    }

    // We can fetch configuration inside processor if needed
    private IConfiguration _configuration => _serviceProvider.GetRequiredService<IConfiguration>();
}
