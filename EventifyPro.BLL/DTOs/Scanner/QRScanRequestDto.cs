namespace EventifyPro.BLL.DTOs.Scanner;

/// <summary>
/// Data transfer object for QR code scanning request.
/// </summary>
public record QRScanRequestDto
{
    /// <summary>
    /// Gets or sets the QR code data to scan.
    /// </summary>
    [Required, StringLength(500)]
    public string QRCode { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the event identifier where the scan is taking place.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public int EventId { get; init; }
}
