using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class EventService : IEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUploadHelper _uploadHelper;

    public EventService(IUnitOfWork unitOfWork, IMapper mapper, IUploadHelper uploadHelper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _uploadHelper = uploadHelper;
    }

    public Task<Result<EventDetailDto>> CreateAsync(EventCreateDto dto, string organizerId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<EventDetailDto>> UpdateAsync(int id, EventUpdateDto dto, string organizerId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> PublishAsync(int id, string organizerId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> CancelAsync(int id, string organizerId, string reason, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<EventDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<PagedResult<EventSummaryDto>> SearchAsync(EventFilterDto filter, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> DeleteAsync(int id, string organizerId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
