namespace EventifyPro.BLL.Services.Implementations;

public class ScanLogService : IScanLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ScanLogService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result> LogAsync(int eventId, int? ticketId, int? actualEventId, string result, string scannedById, string? rawQRData, CancellationToken cancellationToken = default)
    {
        // Parse result enum
        if (!Enum.TryParse<ScanResult>(result, out var scanResult))
            return Result.Failure("Invalid scan result.");

        var scanLog = new ScanLog
        {
            EventId = eventId,
            TicketId = ticketId,
            ActualEventId = actualEventId,
            ScannedById = scannedById,
            ScannedAt = DateTime.UtcNow,
            Result = scanResult,
            RawQRCode = rawQRData
        };

        await _unitOfWork.ScanLogs.AddAsync(scanLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<ScanLogResponseDto>>> GetSessionLogsAsync(string scannerId, int eventId, DateTime sessionStart, CancellationToken cancellationToken = default)
    {
        // Get all scan logs for this scanner after session start
        var allLogs = await _unitOfWork.ScanLogs.GetScansByEventAsync(eventId, cancellationToken);
        var sessionLogs = allLogs
            .Where(log => log.ScannedById == scannerId && log.ScannedAt >= sessionStart)
            .OrderByDescending(log => log.ScannedAt)
            .ToList();

        var dtos = new List<ScanLogResponseDto>();
        foreach (var log in sessionLogs)
        {
            var scanner = await _unitOfWork.Users.GetByIdAsync(log.ScannedById, cancellationToken);
            var dto = _mapper.Map<ScanLogResponseDto>(log);
            dto = dto with { ScannerName = scanner?.FullName ?? "Unknown" };
            dtos.Add(dto);
        }

        return Result<IReadOnlyList<ScanLogResponseDto>>.Success(dtos.AsReadOnly());
    }

    public async Task<Result<IReadOnlyList<ScanLogResponseDto>>> GetEventLogsAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var logs = await _unitOfWork.ScanLogs.GetScansByEventAsync(eventId, cancellationToken);
        var orderedLogs = logs.OrderByDescending(log => log.ScannedAt).ToList();

        var dtos = new List<ScanLogResponseDto>();
        foreach (var log in orderedLogs)
        {
            var scanner = await _unitOfWork.Users.GetByIdAsync(log.ScannedById, cancellationToken);
            var dto = _mapper.Map<ScanLogResponseDto>(log);
            dto = dto with { ScannerName = scanner?.FullName ?? "Unknown" };
            dtos.Add(dto);
        }

        return Result<IReadOnlyList<ScanLogResponseDto>>.Success(dtos.AsReadOnly());
    }
}
