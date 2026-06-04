using MapsterMapper;

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

    public Task<Result> LogAsync(int eventId, int? ticketId, int? actualEventId, string result, string scannedById, string? rawQRData, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<IReadOnlyList<ScanLogResponseDto>>> GetSessionLogsAsync(string scannerId, int eventId, DateTime sessionStart, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<IReadOnlyList<ScanLogResponseDto>>> GetEventLogsAsync(int eventId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
