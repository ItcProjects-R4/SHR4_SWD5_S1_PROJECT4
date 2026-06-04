namespace EventifyPro.BLL.Services.Interfaces;

public interface ITicketService
{
    Task<Result<IReadOnlyList<TicketResponseDto>>> GenerateForBookingAsync(int bookingId, CancellationToken cancellationToken = default);
    Task<Result<QRScanResultDto>> ValidateAndUseAsync(QRScanRequestDto dto, string scannerId, CancellationToken cancellationToken = default);
    Task<Result<TicketResponseDto>> GetByIdAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TicketResponseDto>>> GetTicketsByBookingAsync(int bookingId, string userId, CancellationToken cancellationToken = default);
}
