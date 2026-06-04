namespace EventifyPro.BLL.DTOs.Category;

/// <summary>
/// Data transfer object containing category information.
/// </summary>
public record CategoryDto
{
    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the date and time when the category was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the category was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}
