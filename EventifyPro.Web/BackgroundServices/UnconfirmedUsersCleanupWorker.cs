namespace EventifyPro.Web.BackgroundServices
{
    public class UnconfirmedUsersCleanupWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UnconfirmedUsersCleanupWorker> _logger;

        public UnconfirmedUsersCleanupWorker(
            IServiceProvider serviceProvider,
            ILogger<UnconfirmedUsersCleanupWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Unconfirmed Users Cleanup Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking for unconfirmed accounts older than 24 hours...");
                    
                    using var scope = _serviceProvider.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                    var cutoffTime = DateTime.UtcNow.AddDays(-1);
                    var unconfirmedUsers = await userManager.Users
                        .Where(u => !u.EmailConfirmed && u.CreatedAt < cutoffTime)
                        .ToListAsync(stoppingToken);

                    if (unconfirmedUsers.Any())
                    {
                        _logger.LogInformation("Found {Count} unconfirmed users to clean up.", unconfirmedUsers.Count);
                        int deletedCount = 0;

                        foreach (var user in unconfirmedUsers)
                        {
                            var result = await userManager.DeleteAsync(user);
                            if (result.Succeeded)
                            {
                                deletedCount++;
                            }
                            else
                            {
                                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                                _logger.LogWarning("Failed to delete unconfirmed user {Email}. Errors: {Errors}", user.Email, errors);
                            }
                        }

                        _logger.LogInformation("Successfully cleaned up {Count} unconfirmed accounts.", deletedCount);
                    }
                    else
                    {
                        _logger.LogInformation("No unconfirmed accounts found for cleanup.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Unconfirmed Users Cleanup Worker.");
                }

                // Check every 1 hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Unconfirmed Users Cleanup Worker is stopping.");
        }
    }
}
