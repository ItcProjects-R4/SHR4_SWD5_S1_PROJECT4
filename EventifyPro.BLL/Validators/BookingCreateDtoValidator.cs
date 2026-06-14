namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates BookingCreateDto for creating new bookings.
/// Rules: At least one booking item, quantities > 0, valid ticket types.
/// </summary>
public class BookingCreateDtoValidator : AbstractValidator<BookingCreateDto>
{
    public BookingCreateDtoValidator()
    {
        RuleFor(b => b.EventId)
            .GreaterThan(0).WithMessage("Valid event ID is required");

        RuleFor(b => b.Items)
            .NotEmpty().WithMessage("At least one ticket type must be selected")
            .Must(items => items.Any(i => i.Quantity > 0))
                .WithMessage("At least one ticket must be selected");

        RuleForEach(b => b.Items)
            .SetValidator(new BookingItemRequestDtoValidator());
    }
}

/// <summary>
/// Validates BookingItemRequestDto for individual booking items.
/// Rules: TicketTypeId valid, Quantity > 0.
/// </summary>
public class BookingItemRequestDtoValidator : AbstractValidator<BookingItemRequestDto>
{
    public BookingItemRequestDtoValidator()
    {
        RuleFor(bi => bi.TicketTypeId)
            .GreaterThan(0).WithMessage("Valid ticket type ID is required");

        RuleFor(bi => bi.Quantity)
            .GreaterThan(0).WithMessage("Ticket quantity must be at least 1")
            .LessThanOrEqualTo(100).WithMessage("Cannot book more than 100 tickets at once");
    }
}
