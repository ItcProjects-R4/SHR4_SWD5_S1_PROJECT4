using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class OutboxService : IOutboxService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public OutboxService(IUnitOfWork unitOfWork, IEmailService emailService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _mapper = mapper;
    }

    public Task EnqueueAsync(string type, object payload, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task ProcessPendingAsync(CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
