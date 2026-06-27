namespace EventifyPro.BLL.Services.Implementations;

public class ScanLogService : IScanLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ScanLogService> _logger;

    public ScanLogService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ScanLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result> LogAsync(int eventId, int? ticketId, int? actualEventId, string result, string scannedById, string? rawQRData, CancellationToken cancellationToken = default)
    {
        // Parse result enum
        if (!Enum.TryParse<ScanResult>(result, out var scanResult))
        {
            _logger.LogWarning("Failed to parse scan result: {Result}", result);
            return Result.Failure("Invalid scan result.");
        }

        _logger.LogInformation("Recording scan log. Event ID: {EventId}, Ticket ID: {TicketId}, Result: {Result}, Scanned By: {ScannedBy}", 
            eventId, ticketId, scanResult, scannedById);

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

    public async Task<Result<PagedResult<ScanLogResponseDto>>> GetSessionLogsAsync(
        string scannerId, 
        int eventId, 
        DateTime sessionStart, 
        int pageNumber = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var scannerUser = await _unitOfWork.Users.GetByIdAsync(scannerId, cancellationToken);
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (scannerUser == null || !scannerUser.IsActive || eventEntity == null || scannerUser.ScannerCreatedByOrganizerId != eventEntity.OrganizerId)
        {
            return Result<PagedResult<ScanLogResponseDto>>.Failure("You are not authorized to view logs for this event.");
        }

        var isAssigned = await _unitOfWork.DbContext.EventScanners
            .AnyAsync(es => es.ScannerId == scannerId && es.EventId == eventId, cancellationToken);
        if (!isAssigned)
        {
            return Result<PagedResult<ScanLogResponseDto>>.Failure("You are not authorized to view logs for this event.");
        }

        var query = _unitOfWork.DbContext.Set<ScanLog>()
            .AsNoTracking()
            .Where(log => log.EventId == eventId && log.ScannedById == scannerId && log.ScannedAt >= sessionStart);

        var totalCount = await query.CountAsync(cancellationToken);

        var sessionLogs = await query
            .OrderByDescending(log => log.ScannedAt)
            .Include(log => log.Scanner)
            .Include(log => log.Ticket)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = sessionLogs.Select(log =>
        {
            var dto = _mapper.Map<ScanLogResponseDto>(log);
            return dto with { ScannerName = log.Scanner?.FullName ?? "Unknown" };
        }).ToList();

        var pagedResult = PagedResult<ScanLogResponseDto>.Create(dtos, totalCount, pageNumber, pageSize);
        return Result<PagedResult<ScanLogResponseDto>>.Success(pagedResult);
    }

    public async Task<Result<PagedResult<ScanLogResponseDto>>> GetEventLogsAsync(
        int eventId, 
        string userId, 
        bool isAdmin, 
        int pageNumber = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
        {
            return Result<PagedResult<ScanLogResponseDto>>.Failure("Event not found.");
        }

        if (!isAdmin && eventEntity.OrganizerId != userId)
        {
            return Result<PagedResult<ScanLogResponseDto>>.Failure("You are not authorized to view logs for this event.");
        }

        var query = _unitOfWork.DbContext.Set<ScanLog>()
            .AsNoTracking()
            .Where(log => log.EventId == eventId);

        var totalCount = await query.CountAsync(cancellationToken);

        var eventLogs = await query
            .OrderByDescending(log => log.ScannedAt)
            .Include(log => log.Scanner)
            .Include(log => log.Ticket)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = eventLogs.Select(log =>
        {
            var dto = _mapper.Map<ScanLogResponseDto>(log);
            return dto with { ScannerName = log.Scanner?.FullName ?? "Unknown" };
        }).ToList();

        var pagedResult = PagedResult<ScanLogResponseDto>.Create(dtos, totalCount, pageNumber, pageSize);
        return Result<PagedResult<ScanLogResponseDto>>.Success(pagedResult);
    }
}
