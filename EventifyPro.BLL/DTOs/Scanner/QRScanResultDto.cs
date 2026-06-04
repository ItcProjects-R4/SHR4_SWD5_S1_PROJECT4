namespace EventifyPro.BLL.DTOs.Scanner;

/// <summary>
/// Data transfer object containing QR code scan results.
/// </summary>
public record QRScanResultDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the scanned QR code is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets or sets the message or reason for the scan result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the ticket identifier associated with the scanned QR code.
    /// </summary>
    public int? TicketId { get; init; }

    /// <summary>
    /// Gets or sets the name of the ticket type.
    /// </summary>
    public string? TicketTypeName { get; init; }

    /// <summary>
    /// Gets or sets the email address of the ticket attendee.
    /// </summary>
    public string? AttendeeEmail { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the ticket has already been used/scanned.
    /// </summary>
    public bool IsAlreadyUsed { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the ticket was first used.
    /// </summary>
    public DateTime? FirstUsedAt { get; init; }

    /// <summary>
    /// Gets or sets the overall scan result (e.g., Valid, AlreadyUsed, InvalidTicket).
    /// </summary>
    public string ScanResult { get; init; } = string.Empty;
}
