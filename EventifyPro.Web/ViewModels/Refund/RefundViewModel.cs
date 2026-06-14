namespace EventifyPro.Web.ViewModels.Refund;

public class RefundViewModel
{
    public int Id { get; set; }

    public int PaymentId { get; set; }

    public int BookingId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? TransactionId { get; set; }

    public string? Reason { get; set; }

    public string? ProcessedByAdminId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
