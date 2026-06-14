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
        if (string.IsNullOrWhiteSpace(method))
            return PaymentMethod.DummyPayment;

        method = method.Trim();

        // Map Visa, MasterCard, Meeza cards to CreditCard
        if (string.Equals(method, "Visa", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "MasterCard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "Master Card", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "Meeza", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "MeezaCard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "CreditCard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "Card", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentMethod.CreditCard;
        }

        // Map Vodafone Cash and other wallets to MobileWallet
        if (string.Equals(method, "VodafoneCash", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "Vodafone Cash", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "MobileWallet", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "Wallet", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "MeezaWallet", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, "Meeza Digital", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentMethod.MobileWallet;
        }

        return Enum.TryParse<PaymentMethod>(method, ignoreCase: true, out var paymentMethod)
            ? paymentMethod
            : PaymentMethod.DummyPayment;
    }
}

#pragma warning restore CS8603
