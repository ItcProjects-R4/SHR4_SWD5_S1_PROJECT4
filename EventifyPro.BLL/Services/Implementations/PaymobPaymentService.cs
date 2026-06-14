namespace EventifyPro.BLL.Services.Implementations;

/// <summary>
/// Payment service implementation that integrates with the Paymob payment gateway
/// using the Intention API for payment processing.
/// </summary>
public class PaymobPaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly HttpClient _httpClient;
    private readonly PaymobSettings _settings;
    private readonly ILogger<PaymobPaymentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public PaymobPaymentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        HttpClient httpClient,
        IOptions<PaymobSettings> settings,
        ILogger<PaymobPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", _settings.SecretKey);
    }

    /// <inheritdoc />
    public async Task<Result<PaymentResultDto>> InitiateAsync(PaymentInitDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Validate the booking exists and has no existing payment
        var booking = await _unitOfWork.Bookings.GetByIdWithDetailsAsync(dto.BookingId, cancellationToken);
        if (booking is null)
            return Result<PaymentResultDto>.Failure(ErrorMessages.Booking.NotFound);

        var existingPayment = await _unitOfWork.Payments.GetPaymentByBookingAsync(dto.BookingId, cancellationToken);
        if (existingPayment is not null && existingPayment.Status == PaymentStatus.Completed)
            return Result<PaymentResultDto>.Failure("This booking already has a completed payment.");

        // 2. Create the payment record in DB with Pending status
        var payment = _mapper.Map<Payment>(dto);
        payment.Status = PaymentStatus.Pending;
        payment.PaymentDate = DateTime.UtcNow;

        if (existingPayment is not null)
        {
            // Update existing pending payment
            existingPayment.Amount = dto.Amount;
            existingPayment.Method = payment.Method;
            existingPayment.PaymentDate = DateTime.UtcNow;
            existingPayment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Payments.Update(existingPayment);
            payment = existingPayment;
        }
        else
        {
            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Determine integration IDs based on payment method
        var integrationIds = new List<int>();
        var isCard = string.Equals(dto.Method, "CreditCard", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(dto.Method, "Card", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(dto.Method, "Visa", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(dto.Method, "MasterCard", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(dto.Method, "Master Card", StringComparison.OrdinalIgnoreCase);

        var isWallet = string.Equals(dto.Method, "MobileWallet", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "Wallet", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "VodafoneCash", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "Vodafone Cash", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "OrangeCash", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "Orange Cash", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "EtisalatCash", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "Etisalat Cash", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "MeezaWallet", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(dto.Method, "Meeza Digital", StringComparison.OrdinalIgnoreCase);

        var isMeeza = string.Equals(dto.Method, "Meeza", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(dto.Method, "MeezaCard", StringComparison.OrdinalIgnoreCase);

        var isInstapay = string.Equals(dto.Method, "InstaPay", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(dto.Method, "Instapay", StringComparison.OrdinalIgnoreCase);

        if (isMeeza)
        {
            if (int.TryParse(_settings.MeezaIntegrationId, out var meezaId))
            {
                integrationIds.Add(meezaId);
            }
            else if (int.TryParse(_settings.IntegrationId, out var cardId))
            {
                integrationIds.Add(cardId);
            }
        }
        else if (isCard && int.TryParse(_settings.IntegrationId, out var cardId))
        {
            integrationIds.Add(cardId);
        }
        else if (isWallet && int.TryParse(_settings.WalletIntegrationId, out var walletId))
        {
            integrationIds.Add(walletId);
        }
        else if (isInstapay && int.TryParse(_settings.InstapayIntegrationId, out var instapayId))
        {
            integrationIds.Add(instapayId);
        }
        else
        {
            // Default: add all configured integration IDs so they are shown on the Unified Checkout page
            if (int.TryParse(_settings.IntegrationId, out var cardIntegrationId))
                integrationIds.Add(cardIntegrationId);

            if (!string.IsNullOrEmpty(_settings.WalletIntegrationId) &&
                int.TryParse(_settings.WalletIntegrationId, out var walletIntegrationId))
                integrationIds.Add(walletIntegrationId);

            if (!string.IsNullOrEmpty(_settings.MeezaIntegrationId) &&
                int.TryParse(_settings.MeezaIntegrationId, out var meezaIntegrationId))
                integrationIds.Add(meezaIntegrationId);

            if (!string.IsNullOrEmpty(_settings.InstapayIntegrationId) &&
                int.TryParse(_settings.InstapayIntegrationId, out var instapayIntegrationId))
                integrationIds.Add(instapayIntegrationId);
        }

        var amountCents = (int)(dto.Amount * 100); 
        var fullName = booking.User?.FullName ?? "Guest User";
        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : "Guest";
        var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "User";

        var intentionRequest = new
        {
            amount = amountCents,
            currency = _settings.Currency,
            payment_methods = integrationIds.ToArray(),
            special_reference = $"{payment.Id}_{Guid.NewGuid().ToString(format: "N").Substring(0, 8)}", 
            billing_data = new
            {
                first_name = firstName,
                last_name = lastName,
                email = booking.User?.Email ?? "na@na.com",
                phone_number = booking.User?.PhoneNumber ?? "+20000000000",
                apartment = "N/A",
                floor = "N/A",
                street = "N/A",
                building = "N/A",
                shipping_method = "N/A",
                postal_code = "N/A",
                city = "N/A",
                country = "EG",
                state = "N/A"
            },
            items = new[]
            {
                new
                {
                    name = $"Booking #{booking.Id}",
                    amount = amountCents,
                    description = $"Payment for event booking #{booking.Id}",
                    quantity = 1
                }
            },
            extras = new Dictionary<string, string>
            {
                ["payment_id"] = payment.Id.ToString(),
                ["booking_id"] = booking.Id.ToString()
            }
        };

        try
        {
            var jsonContent = JsonSerializer.Serialize(intentionRequest, JsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/intention/", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paymob Intention API failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return Result<PaymentResultDto>.Failure($"Payment gateway error: {response.StatusCode}");
            }

            // Parse response to get client_secret
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var clientSecret = root.GetProperty("client_secret").GetString();
            var paymobIntentionId = root.GetProperty("id").GetString();

            // Update payment with the Paymob intention ID
            payment.TransactionId = paymobIntentionId;
            payment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Payments.Update(payment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Build the checkout URL
            var checkoutUrl = $"{_settings.BaseUrl}/unifiedcheckout/?publicKey={_settings.PublicKey}&clientSecret={clientSecret}";

            var result = _mapper.Map<PaymentResultDto>(payment);
            result = result with { CheckoutUrl = checkoutUrl };

            return Result<PaymentResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Paymob payment intention for booking {BookingId}", dto.BookingId);
            return Result<PaymentResultDto>.Failure("An error occurred while initiating the payment. Please try again.");
        }
    }


    public async Task<Result<PaymentResultDto>> HandleCallbackAsync(string transactionId, bool success, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return Result<PaymentResultDto>.Failure("Transaction ID is required.");

        var payments = await _unitOfWork.Payments.FindAsync(
            p => p.TransactionId == transactionId, cancellationToken);
        var payment = payments.FirstOrDefault();

        if (payment is null)
            return Result<PaymentResultDto>.Failure("Payment not found for the given transaction.");
        
        payment.Status = success ? PaymentStatus.Completed : PaymentStatus.Failed;
        payment.UpdatedAt = DateTime.UtcNow;

        if (success)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(payment.BookingId, cancellationToken);
            if (booking is not null)
            {
                booking.Status = BookingStatus.Confirmed;
                _unitOfWork.Bookings.Update(booking);
            }
        }

        _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PaymentResultDto>.Success(_mapper.Map<PaymentResultDto>(payment));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentResultDto>> HandlePaymobCallbackAsync(
        IDictionary<string, string> callbackData, string hmac, CancellationToken cancellationToken = default)
    {
       
        if (!VerifyHmac(callbackData, hmac))
        {
            _logger.LogWarning("Invalid HMAC signature received in Paymob callback.");
            return Result<PaymentResultDto>.Failure("Invalid callback signature.");
        }

        
        callbackData.TryGetValue("id", out var paymobTransactionId);
        callbackData.TryGetValue("success", out var successStr);

        
        string? paymentIdStr = null;
        if (callbackData.TryGetValue("merchant_order_id", out var merchantOrderId) && !string.IsNullOrEmpty(merchantOrderId))
        {
            paymentIdStr = merchantOrderId;
        }
        else if (callbackData.TryGetValue("special_reference", out var specRef) && !string.IsNullOrEmpty(specRef))
        {
            paymentIdStr = specRef;
        }
        else if (callbackData.TryGetValue("extra_data[payment_id]", out var extraPayId) && !string.IsNullOrEmpty(extraPayId))
        {
            paymentIdStr = extraPayId;
        }
        else if (callbackData.TryGetValue("payment_id", out var payId) && !string.IsNullOrEmpty(payId))
        {
            paymentIdStr = payId;
        }
    
        if (!string.IsNullOrEmpty(paymentIdStr) && paymentIdStr.Contains('_'))
        {
            paymentIdStr = paymentIdStr.Split('_')[0];
        }

        Payment? payment = null;

        if (!string.IsNullOrEmpty(paymentIdStr) && int.TryParse(paymentIdStr, out var paymentId))
        {
            payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
        }
 // Fallback: try to find by stored TransactionId (which could be the Paymob intention ID or order ID)
        if (payment is null)
        {
            if (callbackData.TryGetValue("order", out var orderId) && !string.IsNullOrEmpty(orderId))
            {
                var payments = await _unitOfWork.Payments.FindAsync(
                    p => p.TransactionId == orderId, cancellationToken);
                payment = payments.FirstOrDefault();
            }
        }

        if (payment is null)
        {
            _logger.LogWarning("Payment not found for Paymob callback. merchant_order_id: {MerchantOrderId}, order: {Order}",
                paymentIdStr ?? "N/A", callbackData.TryGetValue("order", out var o) ? o : "N/A");
            return Result<PaymentResultDto>.Failure("Payment not found.");
        }

       
        var isSuccess = successStr?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        payment.Status = isSuccess ? PaymentStatus.Completed : PaymentStatus.Failed;
        payment.TransactionId = paymobTransactionId ?? payment.TransactionId;
        payment.UpdatedAt = DateTime.UtcNow;

    
        if (isSuccess)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(payment.BookingId, cancellationToken);
            if (booking is not null)
            {
                booking.Status = BookingStatus.Confirmed;
                _unitOfWork.Bookings.Update(booking);
            }
        }

        _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Paymob callback processed. PaymentId: {PaymentId}, Status: {Status}",
            payment.Id, payment.Status);

        return Result<PaymentResultDto>.Success(_mapper.Map<PaymentResultDto>(payment));
    }

    /// <summary>
    /// Verifies the HMAC SHA-512 signature of the Paymob callback data.
    /// Supports both GET redirect (lexicographical sorting) and POST webhook (transaction field ordering).
    /// </summary>
    private bool VerifyHmac(IDictionary<string, string> callbackData, string receivedHmac)
    {
        if (string.IsNullOrEmpty(receivedHmac) || string.IsNullOrEmpty(_settings.HmacSecret))
            return false;

        // Strategy A: Flat Lexicographical Sort (GET Redirect)
        var sortedValues = callbackData
            .Where(kvp => !string.Equals(kvp.Key, "hmac", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value);

        var concatenated = string.Join("", sortedValues);
        var calculatedHmac = CalculateSha512Hmac(concatenated, _settings.HmacSecret);

        if (string.Equals(calculatedHmac, receivedHmac, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Strategy B: Paymob Webhook POST Transaction Field Ordering
        var transactionHmacKeys = new[]
        {
            "amount_cents",
            "created_at",
            "currency",
            "error_occured",
            "has_parent_transaction",
            "id",
            "integration_id",
            "is_3d_secure",
            "is_auth",
            "is_capture",
            "is_voided",
            "is_refunded",
            "is_standalone_payment",
            "pending",
            "source_data.pan",
            "source_data.sub_type",
            "source_data.type",
            "success"
        };

        var webhookValues = new List<string>();
        var webhookKeysMissing = false;

        foreach (var key in transactionHmacKeys)
        {
            var value = GetTransactionValue(callbackData, key);
            if (value is null)
            {
                // Fallback default for missing boolean flags to avoid failing calculation
                if (key.StartsWith("is_") || key == "pending" || key == "error_occured" || key == "has_parent_transaction")
                {
                    value = "false";
                }
                else
                {
                    webhookKeysMissing = true;
                    break;
                }
            }
            else
            {
                // Ensure booleans are lowercase "true" or "false"
                var valLower = value.Trim().ToLowerInvariant();
                if (valLower == "true" || valLower == "false")
                {
                    value = valLower;
                }
            }
            webhookValues.Add(value);
        }

        if (!webhookKeysMissing)
        {
            var webhookConcatenated = string.Join("", webhookValues);
            var webhookCalculatedHmac = CalculateSha512Hmac(webhookConcatenated, _settings.HmacSecret);
            if (string.Equals(webhookCalculatedHmac, receivedHmac, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts transaction property values checking potential prefixes and flattening keys.
    /// </summary>
    private string? GetTransactionValue(IDictionary<string, string> callbackData, string key)
    {
        var keysToCheck = new List<string> { key, $"obj.{key}", $"obj[{key}]" };
        
        if (key.StartsWith("source_data."))
        {
            var flatSourceKey = key.Replace(".", "_"); // e.g. source_data_pan
            keysToCheck.Add(flatSourceKey);
            keysToCheck.Add($"obj.{flatSourceKey}");
            keysToCheck.Add($"obj.source_data.{key.Substring("source_data.".Length)}");
        }

        foreach (var k in keysToCheck)
        {
            if (callbackData.TryGetValue(k, out var val) && val is not null)
            {
                return val;
            }
        }

        return null;
    }

    /// <summary>
    /// Calculates SHA-512 HMAC hash.
    /// </summary>
    private string CalculateSha512Hmac(string data, string secret)
    {
        using var hmacAlgo = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmacAlgo.ComputeHash(Encoding.UTF8.GetBytes(data));

        var sb = new StringBuilder();
        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}
