using Eventify.Shared.Wrappers;
using EventifyPro.BLL.DTOs.Scanner;

namespace EventifyPro.BLL.Services.Interfaces;

public interface IScanLogService
{
    Task<Result> LogAsync(int eventId, int? ticketId, int? actualEventId, string result, string scannedById, string? rawQRData, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ScanLogResponseDto>>> GetSessionLogsAsync(string scannerId, int eventId, DateTime sessionStart, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ScanLogResponseDto>>> GetEventLogsAsync(int eventId, string userId, bool isAdmin, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}
