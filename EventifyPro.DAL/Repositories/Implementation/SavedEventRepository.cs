namespace EventifyPro.DAL.Repositories.Implementation;

/// <summary>
/// Repository implementation for SavedEvent entity operations.
/// </summary>
public class SavedEventRepository : GenericRepository<SavedEvent>, ISavedEventRepository
{
    public SavedEventRepository(EventifyDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<SavedEvent>> GetUserSavedEventsAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        return await _dbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Include(s => s.Event)
                .ThenInclude(e => e.Category)
            .Include(s => s.Event)
                .ThenInclude(e => e.Organizer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsEventSavedByUserAsync(
        string userId,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        return await _dbSet
            .AnyAsync(s => s.UserId == userId && s.EventId == eventId, cancellationToken);
    }
}
