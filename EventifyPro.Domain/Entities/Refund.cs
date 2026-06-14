namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a refund transaction initiated against a payment.
/// Tracks refund status, reason, and the user who initiated the refund.
/// </summary>
/// <remarks>
/// Refunds are issued against payments and can be initiated by administrators or system processes.
/// Each refund tracks its processing status, reason for refund, and includes denormalized booking
/// ID for efficient querying of refund history by booking.
/// </remarks>
public class Refund
{
    /// <summary>
    /// Gets or sets the unique identifier for the refund.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the payment ID (foreign key).
    /// </summary>
    /// <value>The associated payment identifier.</value>
    public int PaymentId { get; set; }

    /// <summary>
    /// Gets or sets the booking ID (denormalized for query efficiency).
    /// </summary>
    /// <value>The associated booking identifier.</value>
    public int BookingId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    /// <value>The amount to be refunded.</value>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the current status of the refund.
    /// </summary>
    /// <value>The refund status (Pending, Completed, or Failed).</value>
    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    /// <summary>
    /// Gets or sets the transaction ID from the payment processor for the refund.
    /// </summary>
    /// <value>The external refund transaction identifier, or null if not yet processed.</value>
    public string? TransactionId { get; set; }

    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who initiated the refund.
    /// </summary>
    /// <value>The initiator's user ID.</value>
    public string InitiatedById { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public Payment Payment { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
    public ApplicationUser Initiator { get; set; } = null!;
}