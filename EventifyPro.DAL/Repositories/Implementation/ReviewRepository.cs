namespace EventifyPro.DAL.Repositories.Implementation;

/// <summary>
/// Repository implementation for Review entity operations.
/// Provides methods for querying reviews by event, user, and visibility status.
/// </summary>
public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public ReviewRepository(EventifyDbContext dbContext) : base(dbContext)
    {
    }

  
    public async Task<IReadOnlyList<Review>> GetReviewsByEventAsync(
        int eventId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.User)
            .Include(r => r.Event)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Review>> GetReviewsByEventPagedAsync(
        int eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .Include(r => r.User)
            .Include(r => r.Event)
            .OrderByDescending(r => r.CreatedAt);

        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }


    public async Task<IReadOnlyList<Review>> GetReviewsByUserAsync(
        string userId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        return await _dbSet
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.User)
            .Include(r => r.Event)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Review>> GetReviewsByUserPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return PagedResult<Review>.Empty(pageNumber, pageSize);
        }

        var query = _dbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Include(r => r.User)
            .Include(r => r.Event)
            .OrderByDescending(r => r.CreatedAt);

        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }

  
    public async Task<Review?> GetUserEventReviewAsync(
        string userId, 
        int eventId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _dbSet
            .Where(r => r.UserId == userId && r.EventId == eventId)
            .Include(r => r.User)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(cancellationToken);
    }

   
    public async Task<IReadOnlyList<Review>> GetApprovedReviewsAsync(
        int eventId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EventId == eventId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.User)
            .Include(r => r.Event)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Review>> GetApprovedReviewsPagedAsync(
        int eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(r => r.EventId == eventId && !r.IsHidden)
            .Include(r => r.User)
            .Include(r => r.Event)
            .OrderByDescending(r => r.CreatedAt);

        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetHiddenReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.User)
            .Include(r => r.Event)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }


    public async Task<double> GetAverageRatingAsync(
        int eventId, 
        CancellationToken cancellationToken = default)
    {
        var averageRating = await _dbSet
            .Where(r => r.EventId == eventId && !r.IsHidden)
            .AverageAsync(r => (double)r.Rating, cancellationToken);

        return double.IsNaN(averageRating) ? 0 : averageRating;
    }

   
    public async Task<Dictionary<byte, int>> GetRatingDistributionAsync(
        int eventId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EventId == eventId && !r.IsHidden)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Rating, x => x.Count, cancellationToken);
    }

   
    public async Task<IReadOnlyList<Review>> GetReviewsByDateRangeAsync(
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.CreatedAt >= fromDate && r.CreatedAt <= toDate)
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.User)
            .Include(r => r.Event)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
