

namespace EventifyPro.Web.ViewModels.Scanner;

public class ScanRequestViewModel
{
    [Required, StringLength(500)]
    public string QRCode { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue)]
    public int EventId { get; set; }
}
