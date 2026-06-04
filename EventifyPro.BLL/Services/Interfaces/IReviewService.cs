namespace EventifyPro.BLL.Services.Interfaces;

public interface IReviewService
{
    Task<Result<ReviewResponseDto>> CreateAsync(ReviewCreateDto dto, string userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ReviewResponseDto>>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default);
    Task<Result> HideAsync(int id, string adminId, CancellationToken cancellationToken = default);
}
