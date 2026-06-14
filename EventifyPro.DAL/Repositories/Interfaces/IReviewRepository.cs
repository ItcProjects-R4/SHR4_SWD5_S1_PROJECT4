namespace EventifyPro.DAL.Repositories.Interfaces;

/// <summary>
/// Repository interface for Review entity operations.
/// Inherits from IRepository<Review> and provides Review-specific query operations.
/// </summary>
public interface IReviewRepository : IGenericRepository<Review>
{
    Task<IReadOnlyList<Review>> GetReviewsByEventAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated reviews for an event.
    /// </summary>
    Task<PagedResult<Review>> GetReviewsByEventPagedAsync(
        int eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Review>> GetReviewsByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated reviews written by a user.
    /// </summary>
    Task<PagedResult<Review>> GetReviewsByUserPagedAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Review?> GetUserEventReviewAsync(string userId, int eventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Review>> GetApprovedReviewsAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated approved (visible) reviews for an event.
    /// </summary>
    Task<PagedResult<Review>> GetApprovedReviewsPagedAsync(
        int eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Review>> GetHiddenReviewsAsync(CancellationToken cancellationToken = default);

    Task<double> GetAverageRatingAsync(int eventId, CancellationToken cancellationToken = default);

    Task<Dictionary<byte, int>> GetRatingDistributionAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reviews created or modified within a date range.
    /// </summary>
    /// <remarks>
    /// Retrieves reviews created between fromDate and toDate for reporting and analytics.
    /// </remarks>
    /// <param name="fromDate">The start of the date range (inclusive).</param>
    /// <param name="toDate">The end of the date range (inclusive).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of reviews in the date range.</returns>
    Task<IReadOnlyList<Review>> GetReviewsByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}