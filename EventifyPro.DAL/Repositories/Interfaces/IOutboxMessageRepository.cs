namespace EventifyPro.DAL.Repositories.Interfaces;


public interface IOutboxMessageRepository : IGenericRepository<OutboxMessage>
{
   
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetScheduledMessagesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

   
    Task<IReadOnlyList<OutboxMessage>> GetFailedMessagesAsync(CancellationToken cancellationToken = default);

  
    Task<IReadOnlyList<OutboxMessage>> GetMessagesByTypeAsync(string eventType, CancellationToken cancellationToken = default);

  
    Task<IReadOnlyList<OutboxMessage>> GetMessagesForRetryAsync(int maxRetries = 3, int batchSize = 10, CancellationToken cancellationToken = default);
}
