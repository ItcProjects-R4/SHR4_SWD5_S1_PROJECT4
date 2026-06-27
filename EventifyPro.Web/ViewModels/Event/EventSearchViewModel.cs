namespace EventifyPro.Web.ViewModels.Event;

public class EventSearchViewModel
{
    [StringLength(200)]
    public string? Title { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public DateTime? StartDateFrom { get; set; }

    public DateTime? StartDateTo { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    [StringLength(50)]
    public string? SortBy { get; set; } = "StartDate";

    public bool IsDescending { get; set; }
}
