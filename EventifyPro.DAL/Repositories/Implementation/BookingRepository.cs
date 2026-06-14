namespace EventifyPro.DAL.Repositories.Implementation;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(EventifyDbContext context) : base(context) { }

    public async Task<Booking?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(b => b.Items)
                .ThenInclude(i => i.TicketType)
            .Include(b => b.Tickets)
            .Include(b => b.Payment)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<Booking?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookingReference == reference, cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.Event)
                .ThenInclude(e => e.Category)
            .Include(b => b.Items)
                .ThenInclude(i => i.TicketType)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetConfirmedByEventAsync(int eventId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed)
            .Include(b => b.User)
            .Include(b => b.Items)
                .ThenInclude(i => i.TicketType)
            .OrderBy(b => b.BookingDate)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasConfirmedBookingAsync(string userId, int eventId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AnyAsync(b => b.UserId == userId
                        && b.EventId == eventId
                        && b.Status == BookingStatus.Confirmed,
                      cancellationToken);

    public async Task<decimal> GetTotalRevenueByEventAsync(int eventId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed)
            .SumAsync(b => b.TotalAmount, cancellationToken);

    public async Task<PagedResult<Booking>> GetByUserIdPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.Event)
                .ThenInclude(e => e.Category)
            .Include(b => b.Items)
                .ThenInclude(i => i.TicketType)
            .OrderByDescending(b => b.BookingDate);

        return await query.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }
}