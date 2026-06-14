namespace EventifyPro.Web.ViewModels.Booking;

public class BookingSummaryViewModel
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string BookingReference { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }
}
