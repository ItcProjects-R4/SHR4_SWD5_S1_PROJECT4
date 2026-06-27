namespace EventifyPro.Web.Decorators;

/// <summary>
/// Decorator for IOutboxService that intercepts enqueues and immediately triggers a Hangfire background job
/// to process the outbox, while maintaining BLL decoupling from Hangfire.
/// </summary>
public class HangfireOutboxServiceDecorator : IOutboxService
{
    private readonly OutboxService _inner;
    private readonly IBackgroundJobClient _backgroundJobs;

    public HangfireOutboxServiceDecorator(
        OutboxService inner,
        IBackgroundJobClient backgroundJobs)
    {
        _inner = inner;
        _backgroundJobs = backgroundJobs;
    }

    /// <inheritdoc />
    public Task EnqueueAsync(string type, object payload, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(type, payload, scheduledFor: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(string type, object payload, DateTime? scheduledFor, CancellationToken cancellationToken = default)
    {
        // 1. Write message to database outbox table via standard BLL service
        await _inner.EnqueueAsync(type, payload, scheduledFor, cancellationToken);

        // 2. Trigger Hangfire processing
        if (scheduledFor.HasValue)
        {
            var delay = scheduledFor.Value - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                // Schedule job to execute when the scheduled time arrives
                _backgroundJobs.Schedule<IOutboxService>(
                    x => x.ProcessPendingAsync(CancellationToken.None),
                    delay);
                return;
            }
        }

        // Process immediately for non-delayed messages
        _backgroundJobs.Enqueue<IOutboxService>(
            x => x.ProcessPendingAsync(CancellationToken.None));
    }

    /// <inheritdoc />
    public Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        // Delegate actual processing to BLL OutboxService
        return _inner.ProcessPendingAsync(cancellationToken);
    }
}
