namespace EventifyPro.Web.Extensions;

/// <summary>
/// Extension methods for background services configuration
/// </summary>
public static class BackgroundServicesExtensions
{
    /// <summary>
    /// Adds background hosted services for the application
    /// </summary>
    public static IServiceCollection AddApplicationBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<EventReminderProcessor>();
        services.AddHostedService<WeeklyDigestProcessor>();
        services.AddHostedService<PostEventProcessor>();
        services.AddHostedService<PendingBookingExpirationWorker>();
        services.AddHostedService<UnconfirmedUsersCleanupWorker>();
        services.AddHostedService<WaitingListExpirationProcessor>();

        return services;
    }
}
