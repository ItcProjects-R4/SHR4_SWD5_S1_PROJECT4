using System;
using System.Threading;
using System.Threading.Tasks;
using EventifyPro.BLL.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventifyPro.Web.BackgroundServices
{
    public class PendingBookingExpirationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PendingBookingExpirationWorker> _logger;

        public PendingBookingExpirationWorker(
            IServiceProvider serviceProvider,
            ILogger<PendingBookingExpirationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Pending Booking Expiration Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    
                    var expiredCount = await bookingService.ExpirePendingBookingsAsync(stoppingToken);
                    if (expiredCount > 0)
                      _logger.LogInformation("Expired {Count} pending bookings and released their ticket capacities.", expiredCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Pending Booking Expiration Worker.");
                }

                // Check every 2 minutes
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }

            _logger.LogInformation("Pending Booking Expiration Worker is stopping.");
        }
    }
}
