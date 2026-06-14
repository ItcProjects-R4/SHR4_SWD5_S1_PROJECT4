using System.ComponentModel.DataAnnotations;

namespace EventifyPro.Web.ViewModels.WaitingList;

public class WaitingListJoinViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int TicketTypeId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int QuantityWanted { get; set; }
}
