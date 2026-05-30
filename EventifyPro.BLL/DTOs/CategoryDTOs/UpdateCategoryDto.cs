namespace Eventify.BLL.DTOs.CategoryDTOs;

public record UpdateCategoryDto
(
    int Id,
    string Name,
    string? Description
);