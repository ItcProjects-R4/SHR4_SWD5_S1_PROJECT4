namespace EventifyPro.Web.ViewModels.Payment;

public class PaymentCheckoutViewModel
{
    public int Id { get; set; }

    public string BookingReference { get; set; } = string.Empty;

    public string EventTitle { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal ServiceFee { get; set; }

    public IReadOnlyList<PaymentCheckoutItemViewModel> Items { get; set; } = [];
}
