using System.ComponentModel.DataAnnotations;

namespace EventifyPro.Web.ViewModels.Booking;

public class BookingCreateViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Required, MinLength(1)]
    public List<BookingItemViewModel> Items { get; set; } = [];
}
