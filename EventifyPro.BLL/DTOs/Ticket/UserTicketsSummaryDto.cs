namespace EventifyPro.BLL.DTOs.Ticket
{
    public record UserTicketsSummaryDto
    {
        public PagedResult<TicketResponseDto> Tickets { get; init; } = null!;
        public int ActiveCount { get; init; }
        public int UsedCount { get; init; }
        public int ExpiredCount { get; init; }
        public int CancelledCount { get; init; }
    }
}
