using EventifyPro.BLL.DTOs.SavedEvent;

namespace EventifyPro.BLL.Services.Interfaces;

public interface ISavedEventService
{
    /// <summary>
    /// Saves an event if not saved, or unsaves it if already saved.
    /// Returns true if saved, false if unsaved.
    /// </summary>
    Task<Result<bool>> ToggleSaveEventAsync(string userId, int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated saved events for a specific user.
    /// </summary>
    Task<Result<PagedResult<SavedEventDto>>> GetSavedEventsForUserAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific event is saved by a user.
    /// </summary>
    Task<Result<bool>> IsEventSavedByUserAsync(string userId, int eventId, CancellationToken cancellationToken = default);
}
