namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates WaitingListJoinDto for joining waiting lists.
/// Rules: EventId and TicketTypeId valid, QuantityWanted > 0.
/// </summary>
public class WaitingListJoinDtoValidator : AbstractValidator<WaitingListJoinDto>
{
    public WaitingListJoinDtoValidator()
    {
        RuleFor(w => w.EventId)
            .GreaterThan(0).WithMessage("Valid event ID is required");

        RuleFor(w => w.TicketTypeId)
            .GreaterThan(0).WithMessage("Valid ticket type ID is required");

        RuleFor(w => w.QuantityWanted)
            .GreaterThan(0).WithMessage("Quantity wanted must be at least 1")
            .LessThanOrEqualTo(100).WithMessage("Cannot reserve more than 100 tickets on waiting list");
    }
}
