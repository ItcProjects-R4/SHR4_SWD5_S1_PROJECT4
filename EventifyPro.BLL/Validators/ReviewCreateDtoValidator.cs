namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates ReviewCreateDto for creating event reviews.
/// Rules: Rating 1-5, Comment optional but max 1000 characters, EventId valid.
/// </summary>
public class ReviewCreateDtoValidator : AbstractValidator<ReviewCreateDto>
{
    public ReviewCreateDtoValidator()
    {
        RuleFor(r => r.EventId)
            .GreaterThan(0).WithMessage("Valid event ID is required");

        RuleFor(r => r.Rating)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("Rating must be between 1 and 5 stars");

        RuleFor(r => r.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters")
            .When(r => !string.IsNullOrEmpty(r.Comment));
    }
}
