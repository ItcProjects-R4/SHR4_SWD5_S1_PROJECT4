namespace EventifyPro.BLL.Settings;

/// <summary>
/// Configuration settings for the Paymob payment gateway integration.
/// </summary>
public class PaymobSettings
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Paymob";

    /// <summary>
    /// Secret key for authenticating backend API requests.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Public key for frontend checkout initialization.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// HMAC secret key for verifying webhook callback signatures.
    /// </summary>
    public string HmacSecret { get; set; } = string.Empty;

    /// <summary>
    /// Integration ID for card payments.
    /// </summary>
    public string IntegrationId { get; set; } = string.Empty;

    /// <summary>
    /// Integration ID for mobile wallet payments (optional).
    /// </summary>
    public string WalletIntegrationId { get; set; } = string.Empty;

    /// <summary>
    /// Integration ID for InstaPay payments (optional).
    /// </summary>
    public string InstapayIntegrationId { get; set; } = string.Empty;

    /// <summary>
    /// Integration ID for Meeza card/digital payments (optional).
    /// </summary>
    public string MeezaIntegrationId { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Paymob API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://accept.paymob.com";

    /// <summary>
    /// Default currency for payments.
    /// </summary>
    public string Currency { get; set; } = "EGP";
}
