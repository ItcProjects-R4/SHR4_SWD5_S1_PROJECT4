using MapsterMapper;

namespace EventifyPro.BLL.Services.Implementations;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ITicketService _ticketService;
    private readonly IOutboxService _outboxService;

    public BookingService(IUnitOfWork unitOfWork, IMapper mapper, ITicketService ticketService, IOutboxService outboxService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _ticketService = ticketService;
        _outboxService = outboxService;
    }

    public Task<Result<BookingDetailDto>> CreatePendingAsync(BookingCreateDto dto, string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> ConfirmAsync(int bookingId, string transactionId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result> CancelAsync(int bookingId, string userId, string reason, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<BookingDetailDto>> GetBookingDetailAsync(int bookingId, string userId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<PagedResult<BookingSummaryDto>> GetUserBookingsAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
