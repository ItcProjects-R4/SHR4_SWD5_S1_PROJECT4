namespace EventifyPro.BLL.Services.Implementations;

public class DummyPaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DummyPaymentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Result<PaymentResultDto>> InitiateAsync(PaymentInitDto dto, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<PaymentResultDto>> HandleCallbackAsync(string transactionId, bool success, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public Task<Result<PaymentResultDto>> HandlePaymobCallbackAsync(IDictionary<string, string> callbackData, string hmac, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
