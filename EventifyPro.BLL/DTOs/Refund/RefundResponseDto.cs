namespace EventifyPro.BLL.DTOs.Refund;

/// <summary>
/// Data transfer object containing refund transaction information.
/// </summary>
public record RefundResponseDto
{
    /// <summary>
    /// Gets or sets the refund transaction identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the original payment identifier.
    /// </summary>
    public int PaymentId { get; init; }

    /// <summary>
    /// Gets or sets the booking identifier associated with the refund.
    /// </summary>
    public int BookingId { get; init; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets or sets the refund status (e.g., Pending, Completed, Failed).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment gateway transaction identifier for the refund.
    /// </summary>
    public string? TransactionId { get; init; }

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets the administrator identifier who processed the refund.
    /// </summary>
    public string? ProcessedByAdminId { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the refund was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the refund was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}
