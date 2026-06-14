namespace Eventify.Domain.Entities;

/// <summary>
/// Represents a QR code scan attempt during event entry.
/// Tracks scan result, actual event validation, and raw QR code data for fraud detection.
/// </summary>
/// <remarks>
/// ScanLog records every QR code scan attempt at event entry for audit and fraud detection purposes.
/// The ActualEventId field enables detection of tickets being used at the wrong event, supporting
/// comprehensive security analysis and preventing ticket resale to different events.
/// </remarks>
public class ScanLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the scan log entry.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ticket ID that was scanned (foreign key).
    /// </summary>
    /// <value>The ticket identifier, or null if the QR code was invalid.</value>
    public int? TicketId { get; set; }

    /// <summary>
    /// Gets or sets the event ID where the scan was attempted.
    /// </summary>
    /// <value>The event identifier.</value>
    public int EventId { get; set; }

    /// <summary>
    /// Gets or sets the event ID extracted from the QR code data.
    /// </summary>
    /// <value>The actual event identifier, or null if undecodable.</value>
    public int? ActualEventId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the person who performed the scan.
    /// </summary>
    /// <value>The scanner's user ID.</value>
    public string ScannedById { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the scan occurred.
    /// </summary>
    /// <value>The scan timestamp.</value>
    public DateTime ScannedAt { get; set; }

    /// <summary>
    /// Gets or sets the result of the scan validation.
    /// </summary>
    /// <value>The scan result (Valid, AlreadyUsed, InvalidToken, or WrongEvent).</value>
    public ScanResult Result { get; set; }

    /// <summary>
    /// Gets or sets the raw QR code data as scanned.
    /// </summary>
    /// <value>The raw QR code string for forensic analysis.</value>
    public string? RawQRCode { get; set; }

    public string? Notes { get; set; }

   
    public Ticket? Ticket { get; set; }
    public Event ScanEvent { get; set; } = null!; 
    public Event? ActualEvent { get; set; }             
    public ApplicationUser Scanner { get; set; } = null!;
}