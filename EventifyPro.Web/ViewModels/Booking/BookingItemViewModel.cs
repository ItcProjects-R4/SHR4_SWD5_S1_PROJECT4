using System.ComponentModel.DataAnnotations;

namespace EventifyPro.Web.ViewModels.Booking;

public class BookingItemViewModel
{
    public int Id { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int TicketTypeId { get; set; }

    public string TicketTypeName { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal => Quantity * UnitPrice;
}
