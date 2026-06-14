namespace EventifyPro.BLL.Validators;

/// <summary>
/// Validates PaymentInitDto for initiating payments.
/// Rules: BookingId valid, Amount > 0, valid payment method.
/// </summary>
public class PaymentInitDtoValidator : AbstractValidator<PaymentInitDto>
{
    public PaymentInitDtoValidator()
    {
        RuleFor(p => p.BookingId)
            .GreaterThan(0).WithMessage("Valid booking ID is required");

        RuleFor(p => p.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("Payment amount exceeds maximum allowed");

        RuleFor(p => p.Method)
            .NotEmpty().WithMessage("Payment method is required")
            .MaximumLength(50).WithMessage("Payment method name is invalid");
    }
}
