namespace EventifyPro.BLL.Services.Interfaces;

public interface IScanLogService
{
    Task<Result> LogAsync(int eventId, int? ticketId, int? actualEventId, string result, string scannedById, string? rawQRData, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ScanLogResponseDto>>> GetSessionLogsAsync(string scannerId, int eventId, DateTime sessionStart, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ScanLogResponseDto>>> GetEventLogsAsync(int eventId, CancellationToken cancellationToken = default);
}
