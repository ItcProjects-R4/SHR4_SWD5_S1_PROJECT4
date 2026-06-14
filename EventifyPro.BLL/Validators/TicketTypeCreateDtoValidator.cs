namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates TicketTypeCreateDto for adding ticket types to events.
/// Rules: Price >= 0, TotalQuantity > 0, Unique name per event, sale window optional but valid if present.
/// </summary>
public class TicketTypeCreateDtoValidator : AbstractValidator<TicketTypeCreateDto>
{
    public TicketTypeCreateDtoValidator()
    {
        RuleFor(t => t.EventId)
            .GreaterThan(0).WithMessage("Valid event ID is required");

        RuleFor(t => t.Name)
            .NotEmpty().WithMessage("Ticket type name is required")
            .MaximumLength(100).WithMessage("Ticket type name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("Ticket type name must be at least 2 characters long");

        RuleFor(t => t.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Ticket price cannot be negative")
            .LessThanOrEqualTo(999999.99m).WithMessage("Ticket price exceeds maximum allowed");

        RuleFor(t => t.TotalQuantity)
            .GreaterThan(0).WithMessage("Total quantity must be greater than 0")
            .LessThanOrEqualTo(100000).WithMessage("Total quantity cannot exceed 100000");

        RuleFor(t => t.Description)
            .MaximumLength(500).WithMessage("Ticket description cannot exceed 500 characters")
            .When(t => !string.IsNullOrEmpty(t.Description));

        RuleFor(t => t.SaleStartDate)
            .LessThan(t => t.SaleEndDate)
                .When(t => t.SaleStartDate.HasValue && t.SaleEndDate.HasValue)
                .WithMessage("Sale start date must be before sale end date");

        RuleFor(t => t.SaleEndDate)
            .GreaterThan(t => t.SaleStartDate)
                .When(t => t.SaleEndDate.HasValue && t.SaleStartDate.HasValue)
                .WithMessage("Sale end date must be after sale start date");
    }
}

/// <summary>
/// Validates TicketTypeUpdateDto for updating ticket types.
/// Rules: Same as create, but cannot reduce TotalQuantity below SoldQuantity.
/// </summary>
public class TicketTypeUpdateDtoValidator : AbstractValidator<TicketTypeUpdateDto>
{
    public TicketTypeUpdateDtoValidator()
    {
        RuleFor(t => t.Id)
            .GreaterThan(0).WithMessage("Valid ticket type ID is required");

        RuleFor(t => t.Name)
            .NotEmpty().WithMessage("Ticket type name is required")
            .MaximumLength(100).WithMessage("Ticket type name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("Ticket type name must be at least 2 characters long");

        RuleFor(t => t.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Ticket price cannot be negative")
            .LessThanOrEqualTo(999999.99m).WithMessage("Ticket price exceeds maximum allowed");

        RuleFor(t => t.TotalQuantity)
            .GreaterThan(0).WithMessage("Total quantity must be greater than 0")
            .LessThanOrEqualTo(100000).WithMessage("Total quantity cannot exceed 100000");

        RuleFor(t => t.Description)
            .MaximumLength(500).WithMessage("Ticket description cannot exceed 500 characters")
            .When(t => !string.IsNullOrEmpty(t.Description));

        RuleFor(t => t.SaleStartDate)
            .LessThan(t => t.SaleEndDate)
                .When(t => t.SaleStartDate.HasValue && t.SaleEndDate.HasValue)
                .WithMessage("Sale start date must be before sale end date");

        RuleFor(t => t.SaleEndDate)
            .GreaterThan(t => t.SaleStartDate)
                .When(t => t.SaleEndDate.HasValue && t.SaleStartDate.HasValue)
                .WithMessage("Sale end date must be after sale start date");
    }
}
