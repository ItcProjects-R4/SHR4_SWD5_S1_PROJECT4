using System.ComponentModel.DataAnnotations;

namespace EventifyPro.Web.ViewModels.TicketType;

public class TicketTypeFormViewModel
{
    public int Id { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int TotalQuantity { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime? SaleStartDate { get; set; }

    public DateTime? SaleEndDate { get; set; }
}
