using System.Threading;
using System.Threading.Tasks;

namespace EventifyPro.BLL.Services.Interfaces;

/// <summary>
/// Provides an abstraction to invalidate cached endpoints from business logic layer.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidates all cached event listings and search pages.
    /// </summary>
    Task InvalidateEventCacheAsync(CancellationToken cancellationToken = default);
}
