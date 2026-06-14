using System.ComponentModel.DataAnnotations;

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
}
