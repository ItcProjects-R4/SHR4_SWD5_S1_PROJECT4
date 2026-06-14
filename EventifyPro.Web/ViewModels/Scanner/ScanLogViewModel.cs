namespace EventifyPro.Web.ViewModels.Scanner;

public class ScanLogViewModel
{
    public int Id { get; set; }

    public int? TicketId { get; set; }

    public int EventId { get; set; }

    public int? ActualEventId { get; set; }

    public string ScannedById { get; set; } = string.Empty;

    public string ScannerName { get; set; } = string.Empty;

    public DateTime ScannedAt { get; set; }

    public string ScanResult { get; set; } = string.Empty;

    public string? RawQRData { get; set; }
}
