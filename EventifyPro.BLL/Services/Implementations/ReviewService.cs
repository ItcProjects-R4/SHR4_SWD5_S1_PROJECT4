using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Result<ReviewResponseDto>> CreateAsync(ReviewCreateDto dto, string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<IReadOnlyList<ReviewResponseDto>>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> HideAsync(int id, string adminId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
