namespace EventifyPro.Web.ViewModels.WaitingList;

public class WaitingListViewModel
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public int TicketTypeId { get; set; }

    public string TicketTypeName { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public int QuantityWanted { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
