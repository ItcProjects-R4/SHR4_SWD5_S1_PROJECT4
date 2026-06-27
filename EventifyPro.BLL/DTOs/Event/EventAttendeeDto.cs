namespace EventifyPro.BLL.DTOs.Event;

public record EventAttendeeDto
{
    public int BookingId { get; init; }
    public string AttendeeName { get; init; } = string.Empty;
    public string AttendeeEmail { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime BookingDate { get; init; }
    public List<EventAttendeeTicketDto> Tickets { get; init; } = [];
}

public record EventAttendeeTicketDto
{
    public int TicketId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public bool IsUsed { get; init; }
    public DateTime? UsedAt { get; init; }
}
