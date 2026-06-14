namespace EventifyPro.Web.ViewModels.Ticket;

public class TicketViewModel
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int BookingId { get; set; }

    public int TicketTypeId { get; set; }

    public string TicketTypeName { get; set; } = string.Empty;

    public string QRCode { get; set; } = string.Empty;

    public bool IsUsed { get; set; }

    public string? UsedByName { get; set; }

    public DateTime? UsedAt { get; set; }

    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
