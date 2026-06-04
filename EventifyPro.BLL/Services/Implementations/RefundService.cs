using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IOutboxService _outboxService;

    public RefundService(IUnitOfWork unitOfWork, IMapper mapper, IOutboxService outboxService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _outboxService = outboxService;
    }

    public Task<Result<RefundResponseDto>> InitiateAsync(RefundCreateDto dto, string initiatedByUserId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<IReadOnlyList<RefundResponseDto>>> GetByBookingAsync(int bookingId, string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<decimal>> GetTotalRefundedAsync(int paymentId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
