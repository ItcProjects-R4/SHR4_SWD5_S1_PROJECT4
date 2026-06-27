namespace EventifyPro.BLL.DTOs.Scanner
{
    public record ScannerDetailsDto
    {
        public string Id { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public int TotalScans { get; init; }
        public int ValidScans { get; init; }
        public int InvalidScans { get; init; }
        public PagedResult<ScanLogResponseDto> ScanLogs { get; init; } = null!;
    }
}
