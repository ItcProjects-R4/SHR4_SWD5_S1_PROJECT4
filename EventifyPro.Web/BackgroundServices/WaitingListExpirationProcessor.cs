namespace EventifyPro.Web.BackgroundServices;

public class WaitingListExpirationProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WaitingListExpirationProcessor> _logger;

    public WaitingListExpirationProcessor(IServiceProvider serviceProvider, ILogger<WaitingListExpirationProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Waiting List Expiration Processor is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using (var scope = _serviceProvider.CreateAsyncScope())
                {
                    var waitingListService = scope.ServiceProvider.GetRequiredService<IWaitingListService>();
                    var expiredCount = await waitingListService.ProcessExpiredNotificationsAsync(stoppingToken);
                    if (expiredCount > 0)
                    {
                        _logger.LogInformation("Processed and expired {Count} waiting list notifications.", expiredCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing expired waiting list notifications.");
            }

            // Run the check every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Waiting List Expiration Processor is stopping.");
    }
}
