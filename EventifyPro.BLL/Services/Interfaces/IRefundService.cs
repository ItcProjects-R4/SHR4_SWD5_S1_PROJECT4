using EventifyPro.BLL.DTOs.Refund;

namespace EventifyPro.BLL.Services.Interfaces;

public interface IRefundService
{
    Task<Result<RefundResponseDto>> InitiateAsync(RefundCreateDto dto, string initiatedByUserId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RefundResponseDto>>> GetByBookingAsync(int bookingId, string userId, CancellationToken cancellationToken = default);
    Task<Result<decimal>> GetTotalRefundedAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<Result<RefundInitiationDetailsDto>> GetRefundInitiationDetailsAsync(int bookingId, string userId, CancellationToken cancellationToken = default);
}

