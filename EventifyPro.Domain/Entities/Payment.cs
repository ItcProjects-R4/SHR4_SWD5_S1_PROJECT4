namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a payment made for a booking.
/// Tracks payment method, status, transaction ID, and associated refunds.
/// </summary>
/// <remarks>
/// Each payment is associated with a single booking and tracks the transaction details
/// including the payment method used, current processing status, and any refunds issued
/// against this payment.
/// </remarks>
public class Payment : IAuditable
{
    /// <summary>
    /// Gets or sets the unique identifier for the payment.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the booking ID (foreign key).
    /// </summary>
    /// <value>The associated booking identifier.</value>
    public int BookingId { get; set; }

    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    /// <value>The amount paid.</value>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the payment method used.
    /// </summary>
    /// <value>The payment method (CreditCard, DummyPayment, etc.).</value>
    public PaymentMethod Method { get; set; }

    /// <summary>
    /// Gets or sets the current status of the payment.
    /// </summary>
    /// <value>The payment status (Pending, Completed, Failed, or Refunded).</value>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Gets or sets the transaction ID from the payment processor.
    /// </summary>
    /// <value>The external transaction identifier, or null for local payments.</value>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the currency for the payment (e.g., EGP, USD).
    /// </summary>
    public string Currency { get; set; } = "EGP";

    public DateTime PaymentDate { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Booking Booking { get; set; } = null!;
    public ICollection<Refund> Refunds { get; set; } = [];
}