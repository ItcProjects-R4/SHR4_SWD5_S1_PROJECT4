namespace EventifyPro.BLL.DTOs.Scanner
{
    public record ScannerSummaryDto
    {
        public string Id { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public bool IsActive { get; init; }
        public int TotalScans { get; init; }
        public string? LastScannedEventTitle { get; init; }
        public DateTime? LastScannedAt { get; init; }
        public string? LastScanStatus { get; init; }
    }

    public record ScannerAssignmentDto
    {
        public int EventId { get; init; }
        public string EventTitle { get; init; } = string.Empty;
        public bool IsAssigned { get; init; }
        public DateTime EventStartDate { get; init; }
    }
}
