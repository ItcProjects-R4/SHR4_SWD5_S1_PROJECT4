namespace EventifyPro.BLL.DTOs.Category;

/// <summary>
/// Data transfer object for updating an existing category.
/// </summary>
public record CategoryUpdateDto
{
    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    [Required]
    public int Id { get; init; }

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
