namespace EventifyPro.BLL.Services.Interfaces;

public interface IOutboxService
{
    Task EnqueueAsync(string type, object payload, CancellationToken cancellationToken = default);
    Task EnqueueAsync(string type, object payload, DateTime? scheduledFor, CancellationToken cancellationToken = default);
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}
