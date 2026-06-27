namespace EventifyPro.BLL.DTOs.Scanner;

/// <summary>
/// Data transfer object containing scan log information.
/// </summary>
public record ScanLogResponseDto
{
    /// <summary>
    /// Gets or sets the scan log entry identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets the scanned ticket identifier.
    /// </summary>
    public int? TicketId { get; init; }

    /// <summary>
    /// Gets or sets the event identifier where the scan occurred.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the actual event identifier associated with the ticket.
    /// </summary>
    public int? ActualEventId { get; init; }

    /// <summary>
    /// Gets or sets the user identifier of the scanner.
    /// </summary>
    public string ScannedById { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the scanner.
    /// </summary>
    public string ScannerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the scan occurred.
    /// </summary>
    public DateTime ScannedAt { get; init; }

    /// <summary>
    /// Gets or sets the result of the scan operation.
    /// </summary>
    public string ScanResult { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw QR code data that was scanned.
    /// </summary>
    public string? RawQRData { get; init; }
    public string? EventTitle { get; init; }
    public string? AttendeeName { get; init; }
    public string? Notes { get; init; }
}
