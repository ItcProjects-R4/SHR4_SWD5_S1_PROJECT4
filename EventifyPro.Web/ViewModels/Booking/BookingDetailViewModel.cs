namespace EventifyPro.Web.ViewModels.Booking;

public class BookingDetailViewModel
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string BookingReference { get; set; } = string.Empty;

    public string? CancellationReason { get; set; }

    public DateTime BookingDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<BookingItemViewModel> Items { get; set; } = [];
}
