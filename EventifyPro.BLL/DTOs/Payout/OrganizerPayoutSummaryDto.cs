namespace EventifyPro.BLL.DTOs.Payout
{
    public record OrganizerPayoutSummaryDto
    {
        public decimal TotalEarnings { get; init; }
        public decimal AvailableBalance { get; init; }
        public decimal PendingBalance { get; init; }
        public bool IsBankAccountConnected { get; init; }
        public string BankAccountLast4 { get; init; } = string.Empty;
        public List<PayoutRequestDto> PayoutHistory { get; init; } = [];
    }

    public record PayoutRequestDto
    {
        public int Id { get; init; }
        public decimal Amount { get; init; }
        public DateTime RequestedAt { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
    }
}
