

namespace EventifyPro.Web.ViewModels.OrganizerScanners;

public class ScannerDetailsViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public int TotalScans { get; set; }
    public int ValidScans { get; set; }
    public int InvalidScans { get; set; }
    
    public List<ScanLogItemViewModel> ScanLogs { get; set; } = new();
    
    // Pagination attributes
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ScanLogItemViewModel
{
    public int Id { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string? TicketSerialNumber { get; set; }
    public string? AttendeeName { get; set; }
    public DateTime ScannedAt { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
