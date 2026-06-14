namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates CategoryCreateDto for creating new categories.
/// Rules: Name required, unique, max 100 characters, description optional.
/// </summary>
public class CategoryCreateDtoValidator : AbstractValidator<CategoryCreateDto>
{
    public CategoryCreateDtoValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("Category name must be at least 2 characters long");

        RuleFor(c => c.Description)
            .MaximumLength(500).WithMessage("Category description cannot exceed 500 characters")
            .When(c => !string.IsNullOrEmpty(c.Description));
    }
}

/// <summary>
/// Validates CategoryUpdateDto for updating categories.
/// Rules: Same as create plus valid category ID.
/// </summary>
public class CategoryUpdateDtoValidator : AbstractValidator<CategoryUpdateDto>
{
    public CategoryUpdateDtoValidator()
    {
        RuleFor(c => c.Id)
            .GreaterThan(0).WithMessage("Valid category ID is required");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("Category name must be at least 2 characters long");

        RuleFor(c => c.Description)
            .MaximumLength(500).WithMessage("Category description cannot exceed 500 characters")
            .When(c => !string.IsNullOrEmpty(c.Description));
    }
}
