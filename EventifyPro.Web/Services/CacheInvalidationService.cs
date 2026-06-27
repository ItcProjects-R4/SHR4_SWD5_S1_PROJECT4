namespace EventifyPro.Web.Services;

/// <summary>
/// Web-specific implementation of cache invalidation that evicts items from ASP.NET Core Output Caching.
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IOutputCacheStore _outputCacheStore;

    public CacheInvalidationService(IOutputCacheStore outputCacheStore)
    {
        _outputCacheStore = outputCacheStore;
    }

    public async Task InvalidateEventCacheAsync(CancellationToken cancellationToken = default)
    {
        // Evict all cache entries marked with the "events-cache-tag" tag.
        await _outputCacheStore.EvictByTagAsync("events-cache-tag", cancellationToken);
    }
}
