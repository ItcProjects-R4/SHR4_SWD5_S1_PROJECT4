namespace EventifyPro.BLL.DTOs.Category;

/// <summary>
/// Data transfer object for creating a new category.
/// </summary>
public record CategoryCreateDto
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    [Required, StringLength(100)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the category description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; init; }
}
