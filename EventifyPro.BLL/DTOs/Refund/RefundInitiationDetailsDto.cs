namespace EventifyPro.BLL.DTOs.Refund;

public record RefundInitiationDetailsDto
{
    public int BookingId { get; init; }
    public int PaymentId { get; init; }
    public decimal MaxRefundableAmount { get; init; }
}
