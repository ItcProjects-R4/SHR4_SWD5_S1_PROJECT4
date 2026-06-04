using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class WaitingListService : IWaitingListService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IOutboxService _outboxService;

    public WaitingListService(IUnitOfWork unitOfWork, IMapper mapper, IOutboxService outboxService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _outboxService = outboxService;
    }

    public Task<Result<WaitingListResponseDto>> JoinAsync(WaitingListJoinDto dto, string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> LeaveAsync(int id, string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> NotifyNextAsync(int ticketTypeId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> AdvanceQueueAsync(int eventId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
