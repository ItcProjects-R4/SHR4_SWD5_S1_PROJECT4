using StackExchange.Redis;

namespace EventifyPro.BLL.Services.Implementations
{
    public class DistributedLockService : IDistributedLockService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DistributedLockService> _logger;
        
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _localLocks = new();
        private static readonly ConcurrentDictionary<string, string> _acquiredLockTokens = new();

        public DistributedLockService(IServiceProvider serviceProvider, ILogger<DistributedLockService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            var multiplexer = _serviceProvider.GetService<IConnectionMultiplexer>();
            if (multiplexer != null && multiplexer.IsConnected)
            {
                try
                {
                    var db = multiplexer.GetDatabase();
                    var token = Guid.NewGuid().ToString("N");
                    var acquired = await db.LockTakeAsync(key, token, expiry);
                    if (acquired)
                    {
                        _acquiredLockTokens[key] = token;
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Redis lock acquisition failed for key {Key}. Falling back to in-memory lock.", key);
                }
            }

            var semaphore = _localLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            return await semaphore.WaitAsync(expiry, cancellationToken);
        }

        public async Task ReleaseLockAsync(string key)
        {
            var multiplexer = _serviceProvider.GetService<IConnectionMultiplexer>();
            if (multiplexer != null && multiplexer.IsConnected && _acquiredLockTokens.TryRemove(key, out var token))
            {
                try
                {
                    var db = multiplexer.GetDatabase();
                    await db.LockReleaseAsync(key, token);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Redis lock release failed for key {Key}.", key);
                }
            }

            if (_localLocks.TryGetValue(key, out var semaphore))
            {
                try
                {
                    semaphore.Release();
                }
                catch (SemaphoreFullException)
                {
                    // Already released or not locked
                }
            }
        }
    }
}
