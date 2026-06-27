namespace EventifyPro.DAL.Repositories;

public class EventRepository : GenericRepository<Event>, IEventRepository
{
    public EventRepository(EventifyDbContext context) : base(context) { }

    public async Task<Event?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsSplitQuery()
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    // Tracked — caller may update status

    public async Task<Event?> GetByIdWithTicketTypesAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    // Must be tracked — RowVersion concurrency on TicketType needs change tracking

    public async Task<PagedResult<Event>> GetPublishedPagedAsync(
        string? searchTerm,
        int? categoryId,
        string? city,
        DateTime? from,
        DateTime? to,
        int pageNumber,
        int pageSize,
        bool? isFeatured = null,
        CancellationToken cancellationToken = default)
    {
        // Build base query — Global QueryFilter (!IsDeleted) applies automatically
        var query = _dbSet
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published);

        // Apply optional filters — each adds a SQL AND clause
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(e => e.Title.Contains(searchTerm));

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(e => e.City == city);

        if (from.HasValue)
            query = query.Where(e => e.StartDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartDate <= to.Value);

        if (isFeatured.HasValue)
            query = query.Where(e => e.IsFeatured == isFeatured.Value);

        // Apply ordering BEFORE pagination (important!)
        query = query.OrderBy(e => e.StartDate);

        // Apply includes AFTER ordering but in the same query
        query = query
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(e => e.Category)
                    .Include(e => e.Organizer)
                    .Include(e => e.TicketTypes);

        // Use QueryableExtensions for clean pagination
        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedResult<Event>> GetPendingReviewAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Where(e => e.Status == EventStatus.PendingReview)
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.TicketTypes)
            .OrderBy(e => e.CreatedAt);

        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetByOrganizerIdAsync(string organizerId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Where(e => e.OrganizerId == organizerId)
            .Include(e => e.TicketTypes)
            .Include(e => e.Category)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Event>> GetReviewableByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var reviewedEventIds = _context.Set<Review>()
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId)
            .Select(r => r.EventId);

        return await _dbSet
            .AsNoTracking()
            .Where(e => e.EndDate < now
                     && e.Bookings.Any(b => b.UserId == userId
                                         && b.Status == BookingStatus.Confirmed)
                     && !reviewedEventIds.Contains(e.Id))
            .Include(e => e.Category)
            .OrderByDescending(e => e.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsOrganizerOwnerAsync(int eventId, string organizerId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AnyAsync(e => e.Id == eventId && e.OrganizerId == organizerId, cancellationToken);
    // SQL EXISTS — no entity loading, no IsDeleted concern (GlobalQueryFilter applies)
}
