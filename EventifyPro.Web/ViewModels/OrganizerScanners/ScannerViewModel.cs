
namespace EventifyPro.Web.ViewModels.OrganizerScanners
{
    public class ScannerViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        // Scanner Activity Tracking
        public int TotalScans { get; set; }
        public string? LastScannedEventTitle { get; set; }
        public DateTime? LastScannedAt { get; set; }
        public string? LastScanStatus { get; set; }
    }
}
