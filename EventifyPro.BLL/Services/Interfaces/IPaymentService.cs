namespace EventifyPro.BLL.Services.Interfaces;

/// <summary>
/// Service interface for managing payment operations via Paymob payment gateway.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Initiates a new payment by creating a Paymob Payment Intention and returns the checkout URL.
    /// </summary>
    Task<Result<PaymentResultDto>> InitiateAsync(PaymentInitDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a generic payment callback by transaction ID.
    /// </summary>
    Task<Result<PaymentResultDto>> HandleCallbackAsync(string transactionId, bool success, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles the Paymob webhook/callback with HMAC signature verification.
    /// </summary>
    Task<Result<PaymentResultDto>> HandlePaymobCallbackAsync(IDictionary<string, string> callbackData, string hmac, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles the Paymob GET redirect callback using the payment ID from the URL path.
    /// This is the preferred method — no Paymob API call needed.
    /// </summary>
    Task<Result<PaymentResultDto>> HandleRedirectCallbackAsync(int paymentId, string transactionId, string hmac, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles the Paymob GET redirect callback by looking up the payment from redirect query data.
    /// Fallback method when payment ID is not in the URL path.
    /// </summary>
    Task<Result<PaymentResultDto>> HandleRedirectCallbackAsync(string transactionId, IDictionary<string, string> redirectData, string hmac, CancellationToken cancellationToken = default);
}
