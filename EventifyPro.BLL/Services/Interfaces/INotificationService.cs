using Eventify.Domain.Enums;
using Eventify.Shared.Wrappers;
using EventifyPro.BLL.DTOs.Notification;

namespace EventifyPro.BLL.Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Gets the latest notifications for a specific user.
    /// </summary>
    Task<Result<IReadOnlyList<NotificationDto>>> GetUserNotificationsAsync(string userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread notifications for a specific user.
    /// </summary>
    Task<Result<int>> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all unread notifications for a specific user as read.
    /// </summary>
    Task<Result<bool>> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a specific notification for a user as read.
    /// </summary>
    Task<Result<bool>> MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new notification for a specific user.
    /// </summary>
    Task<Result<bool>> AddNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationType type,
        string? redirectUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds 6 demo notifications (Booking Confirmed, Payment Failed, Event Tomorrow, Ticket Scanned, Review Reminder, Refund Status)
    /// if the user has no notifications yet.
    /// </summary>
    Task<Result<bool>> SeedDemoNotificationsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to all users in the system.
    /// </summary>
    Task<Result<bool>> SendSystemNotificationAsync(
        string title,
        string message,
        NotificationType type,
        string? redirectUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to a specific user by their email address.
    /// </summary>
    Task<Result<bool>> SendTargetedNotificationAsync(
        string email,
        string title,
        string message,
        NotificationType type,
        string? redirectUrl = null,
        CancellationToken cancellationToken = default);
}
