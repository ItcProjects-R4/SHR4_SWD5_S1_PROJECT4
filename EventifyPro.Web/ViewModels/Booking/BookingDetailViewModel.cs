namespace EventifyPro.Web.ViewModels.Booking;

public class BookingDetailViewModel
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal ServiceFee { get; set; }

    public string Status { get; set; } = string.Empty;

    public string BookingReference { get; set; } = string.Empty;

    public string? CancellationReason { get; set; }

    public DateTime BookingDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<BookingItemViewModel> Items { get; set; } = [];

    // Timeline Tracking Properties
    public DateTime? PaymentDate { get; set; }
    public DateTime? TicketsGeneratedDate { get; set; }
    public DateTime EventStartDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public string EventLocation { get; set; } = string.Empty;
    public string EventCity { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public DateTime? TicketScannedDate { get; set; }
    public bool IsPaymentConfirmed { get; set; }
    public bool AreTicketsGenerated { get; set; }
    public bool IsEventPassed { get; set; }
    public bool IsTicketScanned { get; set; }
}
