using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventifyPro.BLL.DTOs.Ticket;
using EventifyPro.BLL.DTOs.Scanner;
using Eventify.Shared.Wrappers;

namespace EventifyPro.BLL.Services.Interfaces
{
    public interface ITicketService
    {
        Task<Result<IReadOnlyList<TicketResponseDto>>> GenerateForBookingAsync(int bookingId, CancellationToken cancellationToken = default);
        Task<Result<QRScanResultDto>> ValidateAndUseAsync(QRScanRequestDto dto, string scannerId, CancellationToken cancellationToken = default);
        Task<Result<TicketResponseDto>> GetByIdAsync(int id, string userId, CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<TicketResponseDto>>> GetTicketsByBookingAsync(int bookingId, string userId, CancellationToken cancellationToken = default);
        Task<Result<UserTicketsSummaryDto>> GetUserTicketsAsync(string userId, string status, string search, int page, int pageSize, CancellationToken cancellationToken = default);
    }
}
