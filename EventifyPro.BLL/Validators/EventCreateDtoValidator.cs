namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates EventCreateDto for creating new events.
/// Rules: Title required, EndDate > StartDate, StartDate in future, required fields populated.
/// </summary>
public class EventCreateDtoValidator : AbstractValidator<EventCreateDto>
{
    public EventCreateDtoValidator()
    {
        RuleFor(e => e.Title)
            .NotEmpty().WithMessage("Event title is required")
            .MaximumLength(200).WithMessage("Event title cannot exceed 200 characters")
            .MinimumLength(5).WithMessage("Event title must be at least 5 characters long");

        RuleFor(e => e.Description)
            .NotEmpty().WithMessage("Event description is required")
            .MinimumLength(20).WithMessage("Event description must be at least 20 characters long")
            .MaximumLength(5000).WithMessage("Event description cannot exceed 5000 characters");

        RuleFor(e => e.StartDate)
            .NotEmpty().WithMessage("Event start date is required")
            .Must(d => d.UserInputToUtc() > DateTime.UtcNow).WithMessage("Event start date must be in the future");

        RuleFor(e => e.EndDate)
            .NotEmpty().WithMessage("Event end date is required")
            .GreaterThan(e => e.StartDate).WithMessage("Event end date must be after start date");

        RuleFor(e => e.Location)
            .NotEmpty().WithMessage("Event location is required")
            .MaximumLength(300).WithMessage("Event location cannot exceed 300 characters");

        RuleFor(e => e.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City name cannot exceed 100 characters");

        RuleFor(e => e.CategoryId)
            .GreaterThan(0).WithMessage("Valid category must be selected");

        RuleFor(e => e.MaxCapacity)
            .GreaterThan(0).When(e => e.MaxCapacity.HasValue)
            .WithMessage("Maximum capacity must be greater than 0 if specified");
    }
}

/// <summary>
/// Validates EventUpdateDto for updating existing events.
/// Rules: Same as create but EndDate validation is relative to existing StartDate.
/// </summary>
public class EventUpdateDtoValidator : AbstractValidator<EventUpdateDto>
{
    public EventUpdateDtoValidator()
    {
        RuleFor(e => e.Id)
            .GreaterThan(0).WithMessage("Valid event ID is required");

        RuleFor(e => e.Title)
            .NotEmpty().WithMessage("Event title is required")
            .MaximumLength(200).WithMessage("Event title cannot exceed 200 characters")
            .MinimumLength(5).WithMessage("Event title must be at least 5 characters long");

        RuleFor(e => e.Description)
            .NotEmpty().WithMessage("Event description is required")
            .MinimumLength(20).WithMessage("Event description must be at least 20 characters long")
            .MaximumLength(5000).WithMessage("Event description cannot exceed 5000 characters");

        RuleFor(e => e.Location)
            .NotEmpty().WithMessage("Event location is required")
            .MaximumLength(300).WithMessage("Event location cannot exceed 300 characters");

        RuleFor(e => e.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City name cannot exceed 100 characters");

        RuleFor(e => e.CategoryId)
            .GreaterThan(0).WithMessage("Valid category must be selected");

        RuleFor(e => e.MaxCapacity)
            .GreaterThan(0).When(e => e.MaxCapacity.HasValue)
            .WithMessage("Maximum capacity must be greater than 0 if specified");
    }
}

/// <summary>
/// Validates EventFilterDto for event search and filtering.
/// Rules: Optional filters with sensible ranges.
/// </summary>
public class EventFilterDtoValidator : AbstractValidator<EventFilterDto>
{
    public EventFilterDtoValidator()
    {
        RuleFor(f => f.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1");

        RuleFor(f => f.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Page size must be at least 1")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");

        RuleFor(f => f.Title)
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters")
            .When(f => !string.IsNullOrEmpty(f.Title));

        RuleFor(f => f.City)
            .MaximumLength(100).WithMessage("City name cannot exceed 100 characters")
            .When(f => !string.IsNullOrEmpty(f.City));

        RuleFor(f => f.CategoryId)
            .GreaterThan(0).WithMessage("Valid category ID required")
            .When(f => f.CategoryId.HasValue);

        RuleFor(f => f.Status)
            .MaximumLength(50).WithMessage("Status filter is invalid")
            .When(f => !string.IsNullOrEmpty(f.Status));

        RuleFor(f => f.StartDateFrom)
            .LessThan(f => f.StartDateTo)
                .When(f => f.StartDateFrom.HasValue && f.StartDateTo.HasValue)
                .WithMessage("Start date from must be before start date to");

        RuleFor(f => f.StartDateTo)
            .GreaterThan(f => f.StartDateFrom)
                .When(f => f.StartDateTo.HasValue && f.StartDateFrom.HasValue)
                .WithMessage("Start date to must be after start date from");
    }
}
