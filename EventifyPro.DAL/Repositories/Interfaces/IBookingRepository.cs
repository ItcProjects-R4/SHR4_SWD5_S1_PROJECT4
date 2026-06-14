namespace EventifyPro.DAL.Repositories.Interfaces;

public interface IBookingRepository : IGenericRepository<Booking>
{
    /// <summary>
    /// Loads booking with Items → TicketType, Tickets, Payment.
    /// Used for booking detail and confirmation pages.
    /// Tracked (not AsNoTracking) to allow status updates.
    /// </summary>
    Task<Booking?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup by human-readable BookingReference — hits IX_Bookings_BookingReference UNIQUE index.
    /// </summary>
    Task<Booking?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>
    /// All bookings for a user with event info and items — for My Tickets page.
    /// </summary>
    Task<IReadOnlyList<Booking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated bookings for a user with event info and items — for My Tickets page with pagination.
    /// Returns PagedResult with booking data and metadata.
    /// </summary>
    Task<PagedResult<Booking>> GetByUserIdPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirmed bookings for a specific event — for organizer's attendee list.
    /// </summary>
    Task<IReadOnlyList<Booking>> GetConfirmedByEventAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a user has a confirmed booking for an event — prerequisite for writing a review.
    /// Uses SQL EXISTS (AnyAsync) — does not load entities.
    /// </summary>
    Task<bool> HasConfirmedBookingAsync(string userId, int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Total revenue for an event — direct SQL SUM, does not load Booking entities.
    /// </summary>
    Task<decimal> GetTotalRevenueByEventAsync(int eventId, CancellationToken cancellationToken = default);
}