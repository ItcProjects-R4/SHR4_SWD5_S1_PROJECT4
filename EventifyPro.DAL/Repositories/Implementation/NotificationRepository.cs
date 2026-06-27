namespace EventifyPro.DAL.Repositories.Implementation;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(EventifyDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(
        string userId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        return await _dbSet
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        return await _dbSet
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var unreadNotifications = await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }
        
        // SaveChangesAsync on UnitOfWork will write updates to the database
    }
}
