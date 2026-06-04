using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IQRService _qrService;
    private readonly IScanLogService _scanLogService;

    public TicketService(IUnitOfWork unitOfWork, IMapper mapper, IQRService qrService, IScanLogService scanLogService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _qrService = qrService;
        _scanLogService = scanLogService;
    }

    public Task<Result<IReadOnlyList<TicketResponseDto>>> GenerateForBookingAsync(int bookingId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<QRScanResultDto>> ValidateAndUseAsync(QRScanRequestDto dto, string scannerId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<TicketResponseDto>> GetByIdAsync(int id, string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<IReadOnlyList<TicketResponseDto>>> GetTicketsByBookingAsync(int bookingId, string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
