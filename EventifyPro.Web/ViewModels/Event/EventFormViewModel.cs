namespace EventifyPro.Web.ViewModels.Event;

public class EventFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required, StringLength(300)]
    public string Location { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Max capacity must be greater than 0")]
    public int? MaxCapacity { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public IReadOnlyList<EventifyPro.Web.ViewModels.Category.CategoryViewModel> Categories { get; set; } = [];

    public List<TicketTypeWizardItemViewModel> Tickets { get; set; } = new();
}

public class TicketTypeWizardItemViewModel
{
    [Required(ErrorMessage = "Ticket name is required"), StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required"), Range(0, 1000000, ErrorMessage = "Price must be between 0 and 1,000,000")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Quantity is required"), Range(1, 100000, ErrorMessage = "Quantity must be between 1 and 100,000")]
    public int TotalQuantity { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime? SaleStartDate { get; set; }
    public DateTime? SaleEndDate { get; set; }
}
