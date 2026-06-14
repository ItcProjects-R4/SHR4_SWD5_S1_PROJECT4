namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates UserUpdateProfileDto for user profile updates.
/// Rules: FullName required and within limits, ProfileImageUrl optional.
/// </summary>
public class UserUpdateProfileDtoValidator : AbstractValidator<UserUpdateProfileDto>
{
    public UserUpdateProfileDtoValidator()
    {
        RuleFor(u => u.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters long");

        RuleFor(u => u.ProfileImageUrl)
            .MaximumLength(500).WithMessage("Profile image URL cannot exceed 500 characters")
            .Must(url => !string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Invalid image URL format")
                .When(u => !string.IsNullOrEmpty(u.ProfileImageUrl));
    }
}
