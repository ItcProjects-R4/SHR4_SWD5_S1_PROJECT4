namespace EventifyPro.DAL.Repositories.Interfaces;

public interface INotificationRepository : IGenericRepository<Notification>
{
    /// <summary>
    /// Gets the latest notifications for a specific user.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(string userId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread notifications for a specific user.
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all unread notifications for a specific user as read.
    /// </summary>
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}
