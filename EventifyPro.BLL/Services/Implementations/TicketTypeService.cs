using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class TicketTypeService : ITicketTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TicketTypeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Result<TicketTypeResponseDto>> AddToEventAsync(int eventId, TicketTypeCreateDto dto, string organizerId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<TicketTypeResponseDto>> UpdateAsync(int id, TicketTypeUpdateDto dto, string organizerId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<IReadOnlyList<TicketTypeResponseDto>>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
