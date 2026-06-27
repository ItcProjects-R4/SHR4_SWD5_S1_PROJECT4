namespace EventifyPro.DAL.Repositories.Interfaces;

/// <summary>
/// Repository interface for SavedEvent entity operations.
/// </summary>
public interface ISavedEventRepository : IGenericRepository<SavedEvent>
{
    /// <summary>
    /// Gets all saved event entries for a specific user with pagination.
    /// </summary>
    Task<IReadOnlyList<SavedEvent>> GetUserSavedEventsAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an event is already saved by a specific user.
    /// </summary>
    Task<bool> IsEventSavedByUserAsync(string userId, int eventId, CancellationToken cancellationToken = default);
}
