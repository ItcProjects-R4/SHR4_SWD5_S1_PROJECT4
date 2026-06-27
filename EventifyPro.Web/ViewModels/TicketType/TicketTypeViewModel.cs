namespace EventifyPro.Web.ViewModels.TicketType;

public class TicketTypeViewModel
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int TotalQuantity { get; set; }

    public int SoldQuantity { get; set; }

    public int AvailableQuantity => TotalQuantity - SoldQuantity;

    public string? Description { get; set; }

    public DateTime? SaleStartDate { get; set; }

    public DateTime? SaleEndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsAvailable => AvailableQuantity > 0 
        && (!SaleStartDate.HasValue || DateTime.UtcNow >= SaleStartDate.Value)
        && (!SaleEndDate.HasValue || DateTime.UtcNow <= SaleEndDate.Value);
}
