

namespace EventifyPro.Web.ViewModels.Organizer;

public class OrganizerAttendeeViewModel
{
    public int BookingId { get; set; }
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime BookingDate { get; set; }
    public List<AttendeeTicketViewModel> Tickets { get; set; } = new();
}

public class AttendeeTicketViewModel
{
    public int TicketId { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
}
