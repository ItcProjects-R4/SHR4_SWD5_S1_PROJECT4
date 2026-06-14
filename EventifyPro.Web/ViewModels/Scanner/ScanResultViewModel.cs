namespace EventifyPro.Web.ViewModels.Scanner;

public class ScanResultViewModel
{
    public bool IsValid { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? TicketId { get; set; }

    public string? TicketTypeName { get; set; }

    public string? AttendeeEmail { get; set; }

    public bool IsAlreadyUsed { get; set; }

    public DateTime? FirstUsedAt { get; set; }

    public string ScanResult { get; set; } = string.Empty;
}
