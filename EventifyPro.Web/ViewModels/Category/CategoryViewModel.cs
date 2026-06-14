namespace EventifyPro.Web.ViewModels.Category;

public class CategoryViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int EventCount { get; set; }
}
