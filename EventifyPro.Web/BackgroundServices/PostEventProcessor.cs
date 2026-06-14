using Eventify.Domain.Enums;
using EventifyPro.BLL.Services.Interfaces;

namespace EventifyPro.Web.BackgroundServices;

public class PostEventProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PostEventProcessor> _logger;

    public PostEventProcessor(IServiceProvider serviceProvider, ILogger<PostEventProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Post Event Processor Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using (var scope = _serviceProvider.CreateAsyncScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<EventifyPro.DAL.Repositories.Interfaces.IUnitOfWork>();
                    var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    
                    await ProcessPostEventFeedbackEmailsAsync(unitOfWork, outboxService, configuration, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while scanning completed events for feedback.");
            }

            // Run post-event scans every 30 minutes
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }

        _logger.LogInformation("Post Event Processor Service is stopping.");
    }

    private async Task ProcessPostEventFeedbackEmailsAsync(
        EventifyPro.DAL.Repositories.Interfaces.IUnitOfWork unitOfWork, 
        IOutboxService outboxService, 
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var dayAgo = utcNow.AddDays(-1);

        // Find events that ended in the last 24 hours (or ended in the past, and we haven't sent feedback yet)
        var finishedEvents = await unitOfWork.DbContext.Events
            .Where(e => (e.Status == EventStatus.Published || e.Status == EventStatus.Completed)
                && !e.IsDeleted 
                && e.EndDate < utcNow
                && e.EndDate >= dayAgo)
            .Include(e => e.Bookings)
                .ThenInclude(b => b.User)
            .ToListAsync(cancellationToken);

        var baseUrl = configuration["BaseUrl"] ?? "https://localhost:7198";

        foreach (var evt in finishedEvents)
        {
            var confirmedBookings = evt.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed && b.User != null)
                .ToList();

            foreach (var booking in confirmedBookings)
            {
                var user = booking.User;

                var emailMatch = $"\"RecipientEmail\":\"{user.Email}\"";
                var eventIdMatch = $"\"EventId\":{evt.Id}";

                // Check if feedback request has already been enqueued
                var feedbackAlreadyQueued = await unitOfWork.DbContext.OutboxMessages
                    .AnyAsync(m => m.Type == "Email.PostEventFeedback" 
                        && m.Payload.Contains(emailMatch) 
                        && m.Payload.Contains(eventIdMatch), cancellationToken);

                if (!feedbackAlreadyQueued)
                {
                    _logger.LogInformation($"Queuing Post-Event Feedback email for user '{user.Email}' on event '{evt.Title}'");

                    var payload = new OutboxService.PostEventFeedbackPayload
                    {
                        RecipientEmail = user.Email!,
                        RecipientName = user.FullName,
                        EventTitle = evt.Title,
                        EventId = evt.Id,
                        FeedbackUrl = $"{baseUrl}/Home/Feedback?eventId={evt.Id}" // Direct feedback link
                    };

                    await outboxService.EnqueueAsync("Email.PostEventFeedback", payload, cancellationToken);
                }
            }
        }
    }
}
