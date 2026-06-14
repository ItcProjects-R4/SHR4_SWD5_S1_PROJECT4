namespace EventifyPro.BLL.DTOs.Payment;

/// <summary>
/// Data transfer object containing payment transaction results.
/// </summary>
public record PaymentResultDto
{
    /// <summary>
    /// Gets or sets the payment transaction identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the booking identifier associated with this payment.
    /// </summary>
    public int BookingId { get; init; }

    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets or sets the payment method used.
    /// </summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment status (e.g., Pending, Completed, Failed, Refunded).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment gateway transaction identifier.
    /// </summary>
    public string? TransactionId { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the payment was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the payment was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets or sets the Paymob Unified Checkout URL for redirecting the user to complete payment.
    /// </summary>
    public string? CheckoutUrl { get; init; }
}
