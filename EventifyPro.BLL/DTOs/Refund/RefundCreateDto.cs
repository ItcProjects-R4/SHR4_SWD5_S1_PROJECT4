namespace EventifyPro.BLL.DTOs.Refund;

/// <summary>
/// Data transfer object for creating a refund request.
/// </summary>
public record RefundCreateDto
{
    /// <summary>
    /// Gets or sets the payment identifier to be refunded.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int PaymentId { get; init; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    [Required, Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    [Required, StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}
