using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DashboardService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Result<OrganizerDashboardDto>> GetOrganizerDashboardAsync(string organizerId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<AdminDashboardDto>> GetAdminDashboardAsync(CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
