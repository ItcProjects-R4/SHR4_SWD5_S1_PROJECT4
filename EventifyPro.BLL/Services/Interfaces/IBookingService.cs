namespace EventifyPro.BLL.Services.Interfaces;

public interface IBookingService
{
    Task<Result<BookingDetailDto>> CreatePendingAsync(BookingCreateDto dto, string userId, CancellationToken cancellationToken = default);
    Task<Result> ConfirmAsync(int bookingId, string transactionId, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(int bookingId, string userId, string reason, CancellationToken cancellationToken = default);
    Task<Result<BookingDetailDto>> GetBookingDetailAsync(int bookingId, string userId, CancellationToken cancellationToken = default);
    Task<PagedResult<BookingSummaryDto>> GetUserBookingsAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
