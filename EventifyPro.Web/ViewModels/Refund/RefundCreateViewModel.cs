using System.ComponentModel.DataAnnotations;

namespace EventifyPro.Web.ViewModels.Refund;

public class RefundCreateViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int PaymentId { get; set; }

    [Required, Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
