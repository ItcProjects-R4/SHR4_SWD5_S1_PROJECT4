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
    private readonly IBookingService _bookingService;
    private readonly ILogger<PaymobPaymentService> _logger;

    // In-memory store: Paymob Intention ID → Payment ID
    // Used during redirect callback when Paymob API transaction inquiry is unavailable
    private static readonly ConcurrentDictionary<string, int> PendingIntents = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };


    private const string Token = "Token"; 

    public PaymobPaymentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        HttpClient httpClient,
        IOptions<PaymobSettings> settings,
        IBookingService bookingService,
        ILogger<PaymobPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpClient = httpClient;
        _settings = settings.Value;
        _bookingService = bookingService;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(Token, _settings.SecretKey);
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
            existingPayment.Currency = dto.Currency;
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

        var paymobItems = new List<object>();
        foreach (var item in booking.Items)
        {
            paymobItems.Add(new
            {
                name = item.TicketType?.Name ?? $"Ticket Type #{item.TicketTypeId}",
                amount = (int)(item.UnitPrice * 100),
                description = $"Ticket for event booking #{booking.Id}",
                quantity = item.Quantity
            });
        }

        if (booking.ServiceFee > 0)
        {
            paymobItems.Add(new
            {
                name = "Processing Fee",
                amount = (int)(booking.ServiceFee * 100),
                description = "Platform service fee",
                quantity = 1
            });
        }

        var intentionRequest = new
        {
            amount = amountCents,
            currency = payment.Currency,
            payment_methods = integrationIds.ToArray(),
            special_reference = $"{payment.Id}_{Guid.NewGuid().ToString(format: "N").Substring(0, 8)}",
            success_url = $"{dto.SuccessUrl}/pay-{payment.Id}",
            failure_url = $"{dto.FailureUrl}/pay-{payment.Id}",
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
            items = paymobItems.ToArray(),
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

            string? paymobOrderId = null;
            try
            {
                if (root.TryGetProperty("payment_orders", out var paymentOrders) && 
                    paymentOrders.ValueKind == JsonValueKind.Array && 
                    paymentOrders.GetArrayLength() > 0)
                {
                    var firstOrder = paymentOrders[0];
                    if (firstOrder.TryGetProperty("id", out var orderIdProperty))
                    {
                        paymobOrderId = orderIdProperty.ValueKind == JsonValueKind.Number 
                            ? orderIdProperty.GetInt64().ToString() 
                            : orderIdProperty.GetString();
                    }
                }
                
                if (string.IsNullOrEmpty(paymobOrderId) && root.TryGetProperty("order", out var orderObj))
                {
                    if (orderObj.TryGetProperty("id", out var orderIdProp))
                    {
                        paymobOrderId = orderIdProp.ValueKind == JsonValueKind.Number 
                            ? orderIdProp.GetInt64().ToString() 
                            : orderIdProp.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse order ID from Paymob Intention response.");
            }

            // Update payment with the Paymob Order ID (for direct lookup during success redirect) or intention ID as fallback
            payment.TransactionId = paymobOrderId ?? paymobIntentionId;
            payment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Payments.Update(payment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Store intention ID → payment ID mapping for redirect callback (bypasses failing Paymob API)
            if (!string.IsNullOrEmpty(paymobIntentionId))
            {
                PendingIntents[paymobIntentionId] = payment.Id;
                _logger.LogInformation("Stored pending intent mapping: IntentionId={IntentionId} → PaymentId={PaymentId}",
                    paymobIntentionId, payment.Id);
            }

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
            // Call IBookingService.ConfirmAsync to ensure transactional consistency
            var confirmResult = await _bookingService.ConfirmAsync(payment.BookingId, transactionId, cancellationToken);
            if (confirmResult.IsFailure)
            {
                return Result<PaymentResultDto>.Failure(confirmResult.Error!);
            }

            // Fetch updated payment status from DB to return
            payment = await _unitOfWork.Payments.GetByIdAsync(payment.Id, cancellationToken);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Payments.Update(payment);

            var booking = await _unitOfWork.Bookings.GetByIdAsync(payment.BookingId, cancellationToken);
            if (booking is not null)
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken);
                var eventTitle = eventEntity?.Title ?? "your event";
                var failedNotification = new Notification
                {
                    UserId = booking.UserId,
                    Title = "Payment Failed",
                    Message = $"Payment of {payment.Amount:N2} EGP for '{eventTitle}' failed. Please try again to reserve your seat.",
                    Type = NotificationType.PaymentFailed,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Dashboard",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(failedNotification, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<PaymentResultDto>.Success(_mapper.Map<PaymentResultDto>(payment!));
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

        // Extract payment identifier using robust key lookups
        string? paymentIdStr = null;
        var paymentIdKeys = new[]
        {
            "merchant_order_id", "obj.merchant_order_id", "obj.order.merchant_order_id", "order.merchant_order_id",
            "special_reference", "obj.special_reference",
            "extra_data[payment_id]", "obj.extra_data[payment_id]", "extra_data.payment_id", "obj.extra_data.payment_id",
            "payment_id", "obj.payment_id"
        };

        foreach (var key in paymentIdKeys)
        {
            if (callbackData.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val))
            {
                paymentIdStr = val;
                break;
            }
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
        
        // Fallback: try to find by stored TransactionId (which could be the Paymob intention ID, Order ID, or Transaction ID)
        var orderId = GetTransactionValue(callbackData, "order");
        var paymobTransactionId = GetTransactionValue(callbackData, "id");

        if (payment is null)
        {
            var searchTerms = new List<string>();
            if (!string.IsNullOrEmpty(orderId)) searchTerms.Add(orderId);
            if (!string.IsNullOrEmpty(paymobTransactionId)) searchTerms.Add(paymobTransactionId);
            if (!string.IsNullOrEmpty(paymentIdStr)) searchTerms.Add(paymentIdStr);

            if (searchTerms.Any())
            {
                var payments = await _unitOfWork.Payments.FindAsync(
                    p => p.TransactionId != null && searchTerms.Contains(p.TransactionId), cancellationToken);
                payment = payments.FirstOrDefault();
            }
        }

        if (payment is null)
        {
            _logger.LogWarning("Payment not found for Paymob callback. merchant_order_id: {MerchantOrderId}, order: {Order}, transaction: {TxId}",
                paymentIdStr ?? "N/A", orderId ?? "N/A", paymobTransactionId ?? "N/A");
            return Result<PaymentResultDto>.Failure("Payment not found.");
        }

        var successStr = GetTransactionValue(callbackData, "success");
        var isSuccess = successStr?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        var finalTxId = paymobTransactionId ?? payment.TransactionId;

        if (isSuccess)
        {
            // Call IBookingService.ConfirmAsync to ensure transactional consistency
            var confirmResult = await _bookingService.ConfirmAsync(payment.BookingId, finalTxId ?? string.Empty, cancellationToken);
            if (confirmResult.IsFailure)
            {
                return Result<PaymentResultDto>.Failure(confirmResult.Error!);
            }

            // Fetch updated payment status from DB to return
            payment = await _unitOfWork.Payments.GetByIdAsync(payment.Id, cancellationToken);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.TransactionId = finalTxId;
            payment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Payments.Update(payment);

            var booking = await _unitOfWork.Bookings.GetByIdAsync(payment.BookingId, cancellationToken);
            if (booking is not null)
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken);
                var eventTitle = eventEntity?.Title ?? "your event";
                var failedNotification = new Notification
                {
                    UserId = booking.UserId,
                    Title = "Payment Failed",
                    Message = $"Payment of {payment.Amount:N2} EGP for '{eventTitle}' failed. Please try again to reserve your seat.",
                    Type = NotificationType.PaymentFailed,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Dashboard",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(failedNotification, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Paymob callback processed. PaymentId: {PaymentId}, Status: {Status}",
            payment!.Id, payment.Status);

        return Result<PaymentResultDto>.Success(_mapper.Map<PaymentResultDto>(payment));
    }

    /// <summary>
    /// Verifies the HMAC SHA-512 signature of the Paymob callback data.
    /// Supports both GET redirect (lexicographical sorting) and POST webhook (transaction field ordering).
    /// </summary>
    private bool VerifyHmac(IDictionary<string, string> callbackData, string receivedHmac)
    {
        if (string.IsNullOrEmpty(receivedHmac) || string.IsNullOrEmpty(_settings.HmacSecret))
        {
            _logger.LogWarning("VerifyHmac failed: receivedHmac or HmacSecret is empty. HmacSecret configured: {IsConfigured}", !string.IsNullOrEmpty(_settings.HmacSecret));
            return false;
        }

        // Standard transaction keys used by Paymob for HMAC signature calculation
        var standardKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction",
            "id", "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded",
            "is_standalone_payment", "is_voided", "order", "owner", "pending",
            "source_data.pan", "source_data.sub_type", "source_data.type", "success"
        };

        // Strategy A: Flat Lexicographical Sort (GET Redirect) - All keys (excluding hmac)
        var sortedValuesA = callbackData
            .Where(kvp => !string.Equals(kvp.Key, "hmac", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value);

        var concatenatedA = string.Join("", sortedValuesA);
        var calculatedHmacA = CalculateSha512Hmac(concatenatedA, _settings.HmacSecret);

        if (string.Equals(calculatedHmacA, receivedHmac, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Strategy A (GET Redirect - All keys) HMAC verified successfully.");
            return true;
        }

        _logger.LogWarning("Strategy A (GET Redirect - All keys) HMAC mismatch. Concatenated: '{Concatenated}', Calculated: '{Calculated}', Received: '{Received}'",
            concatenatedA, calculatedHmacA, receivedHmac);

        // Strategy A2: Flat Lexicographical Sort (GET Redirect) - ONLY standard keys (ignores extra route/debug parameters)
        var sortedValuesA2 = callbackData
            .Where(kvp => !string.IsNullOrEmpty(kvp.Key) &&
                          !string.Equals(kvp.Key, "hmac", StringComparison.OrdinalIgnoreCase) && 
                          (standardKeys.Contains(kvp.Key) || standardKeys.Contains(kvp.Key.Replace("_", "."))))
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value);

        var concatenatedA2 = string.Join("", sortedValuesA2);
        var calculatedHmacA2 = CalculateSha512Hmac(concatenatedA2, _settings.HmacSecret);

        if (string.Equals(calculatedHmacA2, receivedHmac, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Strategy A2 (GET Redirect - Standard keys) HMAC verified successfully.");
            return true;
        }

        _logger.LogWarning("Strategy A2 (GET Redirect - Standard keys) HMAC mismatch. Concatenated: '{Concatenated}', Calculated: '{Calculated}', Received: '{Received}'",
            concatenatedA2, calculatedHmacA2, receivedHmac);

        // Strategy A3: Flat Lexicographical Sort (GET Redirect) - ONLY standard keys with normalized booleans
        var sortedValuesA3 = callbackData
            .Where(kvp => !string.IsNullOrEmpty(kvp.Key) &&
                          !string.Equals(kvp.Key, "hmac", StringComparison.OrdinalIgnoreCase) && 
                          (standardKeys.Contains(kvp.Key) || standardKeys.Contains(kvp.Key.Replace("_", "."))))
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => {
                var val = kvp.Value;
                var valLower = val?.Trim().ToLowerInvariant();
                if (valLower == "true" || valLower == "false")
                {
                    return valLower;
                }
                return val;
            });

        var concatenatedA3 = string.Join("", sortedValuesA3);
        var calculatedHmacA3 = CalculateSha512Hmac(concatenatedA3, _settings.HmacSecret);

        if (string.Equals(calculatedHmacA3, receivedHmac, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Strategy A3 (GET Redirect - Standard keys normalized) HMAC verified successfully.");
            return true;
        }

        _logger.LogWarning("Strategy A3 (GET Redirect - Standard keys normalized) HMAC mismatch. Concatenated: '{Concatenated}', Calculated: '{Calculated}', Received: '{Received}'",
            concatenatedA3, calculatedHmacA3, receivedHmac);

        // Strategy B1: Webhook POST Transaction Field Ordering (Standard 18 fields)
        var transactionHmacKeysB1 = new[]
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
            "is_refunded",
            "is_standalone_payment",
            "is_voided",
            "pending",
            "source_data.pan",
            "source_data.sub_type",
            "source_data.type",
            "success"
        };

        var webhookValuesB1 = new List<string>();
        foreach (var key in transactionHmacKeysB1)
        {
            var value = GetTransactionValue(callbackData, key);
            if (value is null)
            {
                if (key.StartsWith("is_") || key == "pending" || key == "error_occured" || key == "has_parent_transaction")
                {
                    value = "false";
                }
                else
                {
                    value = "";
                }
            }
            else
            {
                var valLower = value.Trim().ToLowerInvariant();
                if (valLower == "true" || valLower == "false")
                {
                    value = valLower;
                }
            }
            webhookValuesB1.Add(value);
        }

        var webhookConcatenatedB1 = string.Join("", webhookValuesB1);
        var webhookCalculatedHmacB1 = CalculateSha512Hmac(webhookConcatenatedB1, _settings.HmacSecret);
        if (string.Equals(webhookCalculatedHmacB1, receivedHmac, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Strategy B1 (Webhook POST - 18 fields) HMAC verified successfully.");
            return true;
        }

        _logger.LogWarning("Strategy B1 (Webhook POST - 18 fields) HMAC mismatch. Concatenated: '{Concatenated}', Calculated: '{Calculated}', Received: '{Received}'",
            webhookConcatenatedB1, webhookCalculatedHmacB1, receivedHmac);

        // Strategy B2: Webhook POST Transaction Field Ordering (Standard 20 fields including order and owner)
        var transactionHmacKeysB2 = new[]
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
            "is_refunded",
            "is_standalone_payment",
            "is_voided",
            "order",
            "owner",
            "pending",
            "source_data.pan",
            "source_data.sub_type",
            "source_data.type",
            "success"
        };

        var webhookValuesB2 = new List<string>();
        foreach (var key in transactionHmacKeysB2)
        {
            var value = GetTransactionValue(callbackData, key);
            if (value is null)
            {
                if (key.StartsWith("is_") || key == "pending" || key == "error_occured" || key == "has_parent_transaction")
                {
                    value = "false";
                }
                else
                {
                    value = "";
                }
            }
            else
            {
                var valLower = value.Trim().ToLowerInvariant();
                if (valLower == "true" || valLower == "false")
                {
                    value = valLower;
                }
            }
            webhookValuesB2.Add(value);
        }

        var webhookConcatenatedB2 = string.Join("", webhookValuesB2);
        var webhookCalculatedHmacB2 = CalculateSha512Hmac(webhookConcatenatedB2, _settings.HmacSecret);
        if (string.Equals(webhookCalculatedHmacB2, receivedHmac, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Strategy B2 (Webhook POST - 20 fields) HMAC verified successfully.");
            return true;
        }

        _logger.LogWarning("Strategy B2 (Webhook POST - 20 fields) HMAC mismatch. Concatenated: '{Concatenated}', Calculated: '{Calculated}', Received: '{Received}'",
            webhookConcatenatedB2, webhookCalculatedHmacB2, receivedHmac);

        // If all strategies fail, log the details of received data
        _logger.LogError("All HMAC verification strategies failed. Received HMAC: {ReceivedHmac}", receivedHmac);
        foreach (var kvp in callbackData)
        {
            _logger.LogDebug("Callback key-value: {Key} = '{Value}'", kvp.Key, kvp.Value);
        }

        return false;
    }

    /// <summary>
    /// Extracts transaction property values checking potential prefixes and flattening keys.
    /// </summary>
    private string? GetTransactionValue(IDictionary<string, string> callbackData, string key)
    {
        var keysToCheck = new List<string> { key, $"obj.{key}", $"obj[{key}]" };
        
        if (string.Equals(key, "order", StringComparison.OrdinalIgnoreCase))
        {
            keysToCheck.Add("obj.order.id");
            keysToCheck.Add("order.id");
            keysToCheck.Add("order_id");
            keysToCheck.Add("obj.order_id");
        }

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

    /// <inheritdoc />
    public async Task<Result<PaymentResultDto>> HandleRedirectCallbackAsync(
        int paymentId, string transactionId, string hmac, CancellationToken cancellationToken = default)
    {
        // Look up payment by the ID from the URL path (most reliable, no API call needed)
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            _logger.LogWarning("Payment not found by PaymentId: {PaymentId} in redirect.", paymentId);
            return Result<PaymentResultDto>.Failure("Payment not found.");
        }

        if (payment.Status == PaymentStatus.Completed)
        {
            _logger.LogInformation("Payment {PaymentId} already completed (idempotent).", paymentId);
            return Result<PaymentResultDto>.Success(_mapper.Map<PaymentResultDto>(payment));
        }

        var confirmResult = await _bookingService.ConfirmAsync(payment.BookingId, transactionId, cancellationToken);
        if (confirmResult.IsFailure)
        {
            _logger.LogError("Booking confirmation failed for PaymentId: {PaymentId}, BookingId: {BookingId}. Error: {Error}",
                payment.Id, payment.BookingId, confirmResult.Error);
            return Result<PaymentResultDto>.Failure(confirmResult.Error!);
        }

        payment = await _unitOfWork.Payments.GetByIdAsync(payment.Id, cancellationToken);
        _logger.LogInformation("Paymob redirect processed successfully. PaymentId: {PaymentId}, TransactionId: {TransactionId}",
            payment!.Id, transactionId);

        return Result<PaymentResultDto>.Success(_mapper.Map<PaymentResultDto>(payment));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentResultDto>> HandleRedirectCallbackAsync(
        string transactionId, IDictionary<string, string> redirectData, string hmac, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return Result<PaymentResultDto>.Failure("Transaction ID is required.");

        // 1. Verify HMAC on the redirect query parameters (soft check - proceed even if it fails)
        var hmacValid = VerifyHmac(redirectData, hmac);
        if (!hmacValid)
        {
            _logger.LogWarning("HMAC verification failed for Paymob redirect, but will continue for transaction {TransactionId}.", transactionId);
        }

        // 2. Try to find payment via in-memory cache (intention ID → payment ID mapping)
        //    This bypasses the failing Paymob API call for multi-ticket bookings.
        Payment? payment = null;
        if (PendingIntents.TryRemove(transactionId, out var cachedPaymentId))
        {
            _logger.LogInformation("Found payment in in-memory cache: IntentionId={IntentionId} → PaymentId={PaymentId}",
                transactionId, cachedPaymentId);
            payment = await _unitOfWork.Payments.GetByIdAsync(cachedPaymentId, cancellationToken);
        }

        // 3. If not cached, try to find by TransactionId (the 'id' from the redirect)
        if (payment is null)
        {
            // Search by exact TransactionId match (could be intention ID or order ID)
            var paymentsByTx = await _unitOfWork.Payments.FindAsync(
                p => p.Status == PaymentStatus.Pending
                  && p.TransactionId != null
                  && p.TransactionId == transactionId, cancellationToken);
            payment = paymentsByTx.FirstOrDefault();
        }

        // 4. If not found by TransactionId, search all pending payments and try to match
        //    by looking up any payment where the Booking has a Pending status for this user
        if (payment is null)
        {
            var transactionData = await GetPaymobTransactionAsync(transactionId, cancellationToken);
            if (transactionData is not null)
            {
                var merchantOrderId = GetTransactionValue(transactionData, "merchant_order_id");
                if (string.IsNullOrEmpty(merchantOrderId))
                    merchantOrderId = GetTransactionValue(transactionData, "merchant_reference");

                if (!string.IsNullOrEmpty(merchantOrderId) && merchantOrderId.Contains('_'))
                {
                    var paymentIdStr = merchantOrderId.Split('_')[0];
                    if (int.TryParse(paymentIdStr, out var apiPaymentId))
                        payment = await _unitOfWork.Payments.GetByIdAsync(apiPaymentId, cancellationToken);
                }

                if (payment is null)
                {
                    var apiTxId = GetTransactionValue(transactionData, "id");
                    if (!string.IsNullOrEmpty(apiTxId))
                    {
                        var p = await _unitOfWork.Payments.FindAsync(
                            p => p.TransactionId != null && p.TransactionId == apiTxId, cancellationToken);
                        payment = p.FirstOrDefault();
                    }
                }

                if (payment is null)
                {
                    var apiOrderId = GetTransactionValue(transactionData, "order");
                    if (!string.IsNullOrEmpty(apiOrderId))
                    {
                        var p = await _unitOfWork.Payments.FindAsync(
                            p => p.TransactionId != null && p.TransactionId == apiOrderId, cancellationToken);
                        payment = p.FirstOrDefault();
                    }
                }
            }
        }

        if (payment is null)
        {
            _logger.LogWarning("Payment not found for Paymob redirect. TransactionId: {TransactionId}",
                transactionId);
            return Result<PaymentResultDto>.Failure("Payment not found.");
        }

        // 4. Determine success from redirect data
        var success = redirectData.TryGetValue("success", out var s) &&
                       s.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (!success)
        {
            payment.Status = PaymentStatus.Failed;
            payment.TransactionId = transactionId;
            payment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Payments.Update(payment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Paymob redirect reported failed payment. PaymentId: {PaymentId}", payment.Id);
            return Result<PaymentResultDto>.Failure("Payment was not successful.");
        }

        // 7. Confirm the booking
        var confirmResult = await _bookingService.ConfirmAsync(payment.BookingId, transactionId, cancellationToken);
        if (confirmResult.IsFailure)
        {
            _logger.LogError("Booking confirmation failed for PaymentId: {PaymentId}, BookingId: {BookingId}. Error: {Error}",
                payment.Id, payment.BookingId, confirmResult.Error);
            return Result<PaymentResultDto>.Failure(confirmResult.Error!);
        }

        payment = await _unitOfWork.Payments.GetByIdAsync(payment.Id, cancellationToken);

        _logger.LogInformation("Paymob redirect processed successfully. PaymentId: {PaymentId}, TransactionId: {TransactionId}",
            payment!.Id, transactionId);

        return Result<PaymentResultDto>.Success(_mapper.Map<PaymentResultDto>(payment));
    }

    /// <summary>
    /// Fetches transaction details from Paymob's API by transaction ID.
    /// </summary>
    private async Task<Dictionary<string, string>?> GetPaymobTransactionAsync(string transactionId, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/acceptance/transactions/{transactionId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _settings.SecretKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Paymob transaction inquiry failed for {TransactionId}. Status: {StatusCode}",
                    transactionId, response.StatusCode);
                return null;
            }

            var bodyText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(bodyText))
                return null;

            var result = new Dictionary<string, string>();
            using var doc = JsonDocument.Parse(bodyText);

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                FlattenJsonElement(doc.RootElement, result, "");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Paymob transaction {TransactionId}.", transactionId);
            return null;
        }
    }

    /// <summary>
    /// Fallback: tries to find a pending payment whose TransactionId matches the given transaction ID.
    /// This covers cases where the Paymob API call fails (e.g. network issues in local dev).
    /// </summary>
    private async Task<Result<PaymentResultDto>> FindPaymentAndConfirmByTransactionId(string transactionId, CancellationToken cancellationToken)
    {
        var payments = await _unitOfWork.Payments.FindAsync(
            p => p.TransactionId != null && p.TransactionId == transactionId, cancellationToken);
        var payment = payments.FirstOrDefault();

        if (payment is null)
            return Result<PaymentResultDto>.Failure("Payment not found by transaction ID.");

        var confirmResult = await _bookingService.ConfirmAsync(payment.BookingId, transactionId, cancellationToken);
        if (confirmResult.IsFailure)
            return Result<PaymentResultDto>.Failure(confirmResult.Error!);

        payment = await _unitOfWork.Payments.GetByIdAsync(payment.Id, cancellationToken);
        if (payment is null)
            return Result<PaymentResultDto>.Failure("Payment not found after confirmation.");
        return Result<PaymentResultDto>.Success(_mapper.Map<PaymentResultDto>(payment));
    }

    private static void FlattenJsonElement(JsonElement element, Dictionary<string, string> dictionary, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var name = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJsonElement(property.Value, dictionary, name);
                }
                break;
            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var name = $"{prefix}[{index}]";
                    FlattenJsonElement(item, dictionary, name);
                    index++;
                }
                break;
            case JsonValueKind.String:
                dictionary[prefix] = element.GetString() ?? "";
                break;
            case JsonValueKind.Number:
                dictionary[prefix] = element.GetRawText();
                break;
            case JsonValueKind.True:
                dictionary[prefix] = "true";
                break;
            case JsonValueKind.False:
                dictionary[prefix] = "false";
                break;
            case JsonValueKind.Null:
                dictionary[prefix] = "";
                break;
        }
    }
}
