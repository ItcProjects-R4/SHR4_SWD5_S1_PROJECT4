namespace EventifyPro.BLL.DTOs.WaitingList
{
    public record OrganizerWaitingListSummaryDto
    {
        public PagedResult<OrganizerWaitingListEntryDto> Entries { get; init; } = null!;
        public int TotalWaiting { get; init; }
        public int TotalNotified { get; init; }
        public int TotalConverted { get; init; }
        public double ConversionRate { get; init; }
    }

    public record OrganizerWaitingListEntryDto
    {
        public int Id { get; init; }
        public int EventId { get; init; }
        public string EventTitle { get; init; } = string.Empty;
        public string TicketTypeName { get; init; } = string.Empty;
        public int QuantityWanted { get; init; }
        public string AttendeeName { get; init; } = string.Empty;
        public string AttendeeEmail { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime JoinedAt { get; init; }
        public DateTime? NotifiedAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public int PositionInQueue { get; init; }
    }
}
