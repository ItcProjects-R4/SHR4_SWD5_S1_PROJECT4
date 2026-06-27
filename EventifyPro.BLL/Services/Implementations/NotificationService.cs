namespace EventifyPro.BLL.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> GetUserNotificationsAsync(
        string userId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving latest {Count} notifications for user: {UserId}", count, userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<IReadOnlyList<NotificationDto>>.Failure("User ID is required.");
            }

            var notifications = await _unitOfWork.Notifications.GetUserNotificationsAsync(userId, count, cancellationToken);
            var mappedData = notifications.Select(n => _mapper.Map<NotificationDto>(n)).ToList();

            return Result<IReadOnlyList<NotificationDto>>.Success(mappedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user: {UserId}", userId);
            return Result<IReadOnlyList<NotificationDto>>.Failure("An error occurred while retrieving notifications.");
        }
    }

    public async Task<Result<int>> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<int>.Failure("User ID is required.");
            }

            var count = await _unitOfWork.Notifications.GetUnreadCountAsync(userId, cancellationToken);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count for user: {UserId}", userId);
            return Result<int>.Failure("An error occurred while counting unread notifications.");
        }
    }

    public async Task<Result<bool>> MarkAllAsReadAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Marking all notifications as read for user: {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<bool>.Failure("User ID is required.");
            }

            await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully marked all notifications as read for user: {UserId}", userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user: {UserId}", userId);
            return Result<bool>.Failure("An error occurred while marking notifications as read.");
        }
    }

    public async Task<Result<bool>> MarkAsReadAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Marking notification {NotificationId} as read for user: {UserId}", notificationId, userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<bool>.Failure("User ID is required.");
            }

            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null || notification.UserId != userId)
            {
                return Result<bool>.Failure("Notification not found.");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Successfully marked notification {NotificationId} as read for user: {UserId}", notificationId, userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user: {UserId}", notificationId, userId);
            return Result<bool>.Failure("An error occurred while marking the notification as read.");
        }
    }

    public async Task<Result<bool>> AddNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationType type,
        string? redirectUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding notification. UserId: {UserId}, Type: {Type}", userId, type);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<bool>.Failure("User ID is required.");
            }

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                RedirectUrl = redirectUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added notification. Id: {NotificationId}, UserId: {UserId}, Type: {Type}", notification.Id, userId, type);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding notification. UserId: {UserId}, Type: {Type}", userId, type);
            return Result<bool>.Failure("An error occurred while creating the notification.");
        }
    }

    public async Task<Result<bool>> SeedDemoNotificationsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<bool>.Failure("User ID is required.");
            }

            var hasNotifications = await _unitOfWork.Notifications.AnyAsync(n => n.UserId == userId, cancellationToken);
            if (hasNotifications)
            {
                _logger.LogDebug("User {UserId} already has notifications. Seeding skipped.", userId);
                return Result<bool>.Success(false); // Skipped
            }

            _logger.LogInformation("Seeding demo notifications for user: {UserId}", userId);

            var demoNotifications = new List<Notification>
            {
                new()
                {
                    UserId = userId,
                    Title = "Booking Confirmed",
                    Message = "Your booking for the Tech Innovators Summit 2026 has been successfully confirmed! Ticket QR code is ready.",
                    Type = NotificationType.BookingConfirmed,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Dashboard",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new()
                {
                    UserId = userId,
                    Title = "Payment Failed",
                    Message = "Payment of $150.00 for DevFest Cairo 2026 failed. Please try again to reserve your seat.",
                    Type = NotificationType.PaymentFailed,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Dashboard",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new()
                {
                    UserId = userId,
                    Title = "Event Tomorrow",
                    Message = "Don't forget: Startup Grind Summit starts tomorrow at 10:00 AM at the Cairo International Convention Centre.",
                    Type = NotificationType.EventTomorrow,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Dashboard",
                    CreatedAt = DateTime.UtcNow.AddHours(-12)
                },
                new()
                {
                    UserId = userId,
                    Title = "Ticket Scanned",
                    Message = "Welcome! Your ticket was scanned at Gate 3. Enjoy the event.",
                    Type = NotificationType.TicketScanned,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Dashboard",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    UserId = userId,
                    Title = "Review Reminder",
                    Message = "How was your experience at the AI & Robotics Expo? Tap here to leave a review and earn points!",
                    Type = NotificationType.ReviewReminder,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Reviews",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    UserId = userId,
                    Title = "Refund Status",
                    Message = "Refund of $50.00 for the cancelled UX Design Workshop has been processed back to your credit card.",
                    Type = NotificationType.RefundStatus,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Bookings",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            foreach (var notification in demoNotifications)
            {
                await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully seeded 6 demo notifications for user: {UserId}", userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding demo notifications for user: {UserId}", userId);
            return Result<bool>.Failure("An error occurred while seeding demo notifications.");
        }
    }

    public async Task<Result<bool>> SendSystemNotificationAsync(
        string title,
        string message,
        NotificationType type,
        string? redirectUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending system-wide notification: {Title}", title);

            const int batchSize = 500;
            int totalProcessed = 0;
            int skip = 0;
            bool hasMore = true;

            while (hasMore)
            {
                var batch = await _unitOfWork.Users.GetQuery()
                    .AsNoTracking()
                    .OrderBy(u => u.Id)
                    .Skip(skip)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                {
                    hasMore = false;
                    continue;
                }

                var notifications = batch.Select(user => new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    RedirectUrl = redirectUrl,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _unitOfWork.Notifications.AddRangeAsync(notifications, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                totalProcessed += batch.Count;
                skip += batch.Count;
            }

            _logger.LogInformation("Successfully sent system-wide notification to {Count} users: {Title}", totalProcessed, title);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending system-wide notification: {Title}", title);
            return Result<bool>.Failure("An error occurred while sending the system-wide notification.");
        }
    }

    public async Task<Result<bool>> SendTargetedNotificationAsync(
        string email,
        string title,
        string message,
        NotificationType type,
        string? redirectUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending targeted notification to {Email}: {Title}", email, title);
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user == null)
            {
                return Result<bool>.Failure($"User with email {email} was not found.");
            }

            var notification = new Notification
            {
                UserId = user.Id,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                RedirectUrl = redirectUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully sent targeted notification to {Email}: {Title}", email, title);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending targeted notification to {Email}: {Title}", email, title);
            return Result<bool>.Failure("An error occurred while sending the targeted notification.");
        }
    }
}
