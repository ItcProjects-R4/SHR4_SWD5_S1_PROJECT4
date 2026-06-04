namespace EventifyPro.BLL.Services.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentResultDto>> InitiateAsync(PaymentInitDto dto, CancellationToken cancellationToken = default);
    Task<Result<PaymentResultDto>> HandleCallbackAsync(string transactionId, bool success, CancellationToken cancellationToken = default);
}
