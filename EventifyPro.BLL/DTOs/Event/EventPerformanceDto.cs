using System.Collections.Generic;

namespace EventifyPro.BLL.DTOs.Event;

public record EventPerformanceDto
{
    public int EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal TotalRevenue { get; init; }
    public int TotalTicketsSold { get; init; }
    public int TotalCapacity { get; init; }
    public double SoldPercentage { get; init; }
    public int WaitingListCount { get; init; }
    public int ConfirmedBookings { get; init; }
    public int PendingBookings { get; init; }
    public int CancelledBookings { get; init; }
    public List<TicketTypePerformanceDto> TicketTypes { get; init; } = new();
}

public record TicketTypePerformanceDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int TotalQuantity { get; init; }
    public int SoldQuantity { get; init; }
    public double SoldPercentage { get; init; }
}
