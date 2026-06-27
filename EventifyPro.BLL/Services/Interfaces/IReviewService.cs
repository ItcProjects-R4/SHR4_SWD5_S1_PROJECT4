namespace EventifyPro.BLL.Services.Interfaces;

public interface IReviewService
{
    Task<Result<ReviewResponseDto>> CreateAsync(ReviewCreateDto dto, string userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ReviewResponseDto>>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default);
    Task<Result> HideAsync(int id, string adminId, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> CanUserReviewAsync(string userId, int eventId, CancellationToken cancellationToken = default);
    Task<Result<EventReviewsSummaryDto>> GetEventReviewsSummaryAsync(int eventId, CancellationToken cancellationToken = default);
    
    Task<Result<OrganizerReviewsSummaryDto>> GetOrganizerReviewsAsync(
        string organizerId,
        string? searchTerm,
        int? ratingFilter,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result> ReplyToReviewAsync(
        int reviewId,
        string organizerId,
        string replyContent,
        CancellationToken cancellationToken = default);

    Task<Result> FlagReviewAsync(
        int reviewId,
        string organizerId,
        string flaggedReason,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OrganizerReviewItemDto>>> GetOrganizerReviewsForExportAsync(
        string organizerId,
        CancellationToken cancellationToken = default);
}

