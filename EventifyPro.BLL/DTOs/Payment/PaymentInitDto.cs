namespace EventifyPro.BLL.DTOs.Payment;

/// <summary>
/// Data transfer object for initiating a payment transaction.
/// </summary>
public record PaymentInitDto
{
    /// <summary>
    /// Gets or sets the booking identifier associated with this payment.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int BookingId { get; init; }

    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    [Required, Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets or sets the payment method (e.g., Credit Card, PayPal, Bank Transfer).
    /// </summary>
    [Required, StringLength(50)]
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment currency (e.g., EGP, USD).
    /// </summary>
    [Required, StringLength(10)]
    public string Currency { get; init; } = "EGP";

    /// <summary>
    /// Optional success redirect URL passed to Paymob so the user is redirected here after payment.
    /// If not set, Paymob uses the dashboard-configured success URL.
    /// </summary>
    public string? SuccessUrl { get; init; }

    /// <summary>
    /// Optional failure redirect URL passed to Paymob so the user is redirected here on payment failure.
    /// If not set, Paymob uses the dashboard-configured failure URL.
    /// </summary>
    public string? FailureUrl { get; init; }
}
