using Eventify.Domain.Enums;
using EventifyPro.BLL.DTOs.Payment;
using Mapster;

namespace EventifyPro.BLL.Mappings;

#pragma warning disable CS8603 // Mapster Ignore expressions can target nullable members safely.

public sealed class PaymentMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PaymentInitDto, Payment>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.TransactionId)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Booking)
            .Ignore(dest => dest.Refunds)
            .Map(dest => dest.Method, src => ParsePaymentMethod(src.Method))
            .Map(dest => dest.Status, _ => PaymentStatus.Pending)
            .Map(dest => dest.PaymentDate, _ => DateTime.UtcNow);

        config.NewConfig<Payment, PaymentResultDto>()
            .Map(dest => dest.Method, src => src.Method.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.CreatedAt, src => src.PaymentDate);
    }

    private static PaymentMethod ParsePaymentMethod(string method)
    {
        return Enum.TryParse<PaymentMethod>(method, ignoreCase: true, out var paymentMethod)
            ? paymentMethod
            : PaymentMethod.DummyPayment;
    }
}

#pragma warning restore CS8603
