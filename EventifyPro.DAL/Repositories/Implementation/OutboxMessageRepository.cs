namespace EventifyPro.DAL.Repositories.Implementation;

public class OutboxMessageRepository : GenericRepository<OutboxMessage>, IOutboxMessageRepository
{
    public OutboxMessageRepository(EventifyDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        return await _dbSet
            .Where(m => m.ProcessedAt == null && (m.ScheduledFor == null || m.ScheduledFor <= utcNow))
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetScheduledMessagesAsync(
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.ScheduledFor >= fromDate && m.ScheduledFor <= toDate)
            .OrderBy(m => m.ScheduledFor)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

   
    public async Task<IReadOnlyList<OutboxMessage>> GetFailedMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.LastError != null && m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<OutboxMessage>> GetMessagesByTypeAsync(
        string eventType, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return [];
        }

        return await _dbSet
            .Where(m => m.Type == eventType)
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

 
    public async Task<IReadOnlyList<OutboxMessage>> GetMessagesForRetryAsync(
        int maxRetries = 3, 
        int batchSize = 10,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        return await _dbSet
            .Where(m => m.ProcessedAt == null 
                && m.RetryCount < maxRetries 
                && (m.ScheduledFor == null || m.ScheduledFor <= utcNow))
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
