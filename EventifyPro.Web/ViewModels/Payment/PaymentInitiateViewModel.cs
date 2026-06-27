namespace EventifyPro.Web.ViewModels.Payment;

public class PaymentInitiateViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int BookingId { get; set; }

    [Required, Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required, StringLength(50)]
    public string Method { get; set; } = string.Empty;
}
