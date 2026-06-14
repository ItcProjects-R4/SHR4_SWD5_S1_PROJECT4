namespace EventifyPro.DAL.Repositories.Interfaces;

public interface IEventRepository : IGenericRepository<Event>
{
    /// <summary>
    /// Full event details with Category, Organizer, TicketTypes, and average rating.
    /// Tracked — allows status updates after loading.
    /// </summary>
    Task<Event?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event with TicketTypes only — for booking flow stock checks.
    /// Tracked — required for RowVersion concurrency on TicketType.
    /// </summary>
    Task<Event?> GetByIdWithTicketTypesAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated published events with dynamic filters.
    /// Returns PagedResult with filtered events and metadata.
    /// All filter parameters are optional — null means "no filter".
    /// </summary>
    Task<PagedResult<Event>> GetPublishedPagedAsync(
        string? searchTerm,
        int? categoryId,
        string? city,
        DateTime? from,
        DateTime? to,
        int pageNumber,
        int pageSize,
        bool? isFeatured = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Events waiting for admin review.
    /// </summary>
    Task<IReadOnlyList<Event>> GetPendingReviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// All events for an organizer ordered by StartDate descending — for Organizer Dashboard.
    /// </summary>
    Task<IReadOnlyList<Event>> GetByOrganizerIdAsync(string organizerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completed events attended (Confirmed booking) by a user that they haven't reviewed yet.
    /// Used to determine which "Write Review" buttons are enabled.
    /// Uses IgnoreQueryFilters on Reviews to include admin-hidden reviews
    /// (UNIQUE constraint prevents re-submitting even if review is hidden).
    /// </summary>
    Task<IReadOnlyList<Event>> GetReviewableByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates ownership — fast AnyAsync check, no entity loading.
    /// </summary>
    Task<bool> IsOrganizerOwnerAsync(int eventId, string organizerId, CancellationToken cancellationToken = default);
}
