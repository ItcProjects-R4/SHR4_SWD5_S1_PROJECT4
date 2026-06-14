namespace Eventify.Domain.Enums;

/// <summary>
/// Represents the payment method available for booking transactions.
/// </summary>
/// <remarks>
/// Different payment methods are supported by the EventifyPro system, each with its own integration
/// and processing requirements. Additional methods can be added as needed.
/// </remarks>
public enum PaymentMethod : byte
{
    /// <summary>Payment via credit or debit card processed through a payment gateway.</summary>
    CreditCard = 0,
    /// <summary>Dummy payment method used for testing and development purposes.</summary>
    DummyPayment = 1,
    /// <summary>Payment via mobile wallet (e.g., Vodafone Cash, Orange Money).</summary>
    MobileWallet = 2,
    /// <summary>Payment via kiosk (e.g., Aman, Masary).</summary>
    Kiosk = 3,
    /// <summary>Payment via InstaPay.</summary>
    InstaPay = 4
}