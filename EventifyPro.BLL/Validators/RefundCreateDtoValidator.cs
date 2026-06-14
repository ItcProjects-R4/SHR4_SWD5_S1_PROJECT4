namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates RefundCreateDto for initiating refunds.
/// Rules: Amount > 0, Reason optional, PaymentId valid.
/// Note: Total refunds <= Payment.Amount is checked at service level.
/// </summary>
public class RefundCreateDtoValidator : AbstractValidator<RefundCreateDto>
{
    public RefundCreateDtoValidator()
    {
        RuleFor(r => r.PaymentId)
            .GreaterThan(0).WithMessage("Valid payment ID is required");

        RuleFor(r => r.Amount)
            .GreaterThan(0).WithMessage("Refund amount must be greater than 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("Refund amount exceeds maximum allowed");

        RuleFor(r => r.Reason)
            .MaximumLength(500).WithMessage("Refund reason cannot exceed 500 characters")
            .When(r => !string.IsNullOrEmpty(r.Reason));
    }
}
