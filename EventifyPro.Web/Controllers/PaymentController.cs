
namespace EventifyPro.Web.Controllers;

/// <summary>
/// Controller responsible for handling payment operations via Paymob payment gateway.
/// </summary>
public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IBookingService _bookingService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService, 
        IBookingService bookingService, 
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _bookingService = bookingService;
        _logger = logger;
    }


    /// <summary>
    /// Displays the payment checkout/summary page for a booking.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Checkout(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await _bookingService.GetBookingDetailAsync(id, userId, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            TempData["PaymentError"] = result.Error ?? "Booking not found.";
            return RedirectToAction("Index", "Home");
        }

        // Only allow paying for Pending bookings
        if (!string.Equals(result.Data.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["PaymentError"] = "This booking is already paid or cancelled.";
            return RedirectToAction("Index", "Home");
        }

        return View(ToCheckoutViewModel(result.Data));
    }



    /// <summary>
    /// Initiates a payment and redirects to the Paymob checkout page.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [RateLimit(3, 10)]
    public async Task<IActionResult> Initiate(PaymentInitiateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["PaymentError"] = "Invalid payment parameters.";
            return RedirectToAction(nameof(Checkout), new { id = model.BookingId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        // Verify the booking exists, belongs to the current user, and is Pending
        var bookingResult = await _bookingService.GetBookingDetailAsync(model.BookingId, userId, cancellationToken);
        if (!bookingResult.IsSuccess || bookingResult.Data == null)
        {
            TempData["PaymentError"] = bookingResult.Error ?? "Booking not found.";
            return RedirectToAction("Index", "Home");
        }

        if (!string.Equals(bookingResult.Data.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["PaymentError"] = "This booking is already paid or cancelled.";
            return RedirectToAction("Index", "Home");
        }

        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                     (Request.Headers["Accept"].ToString() ?? "").Contains("application/json");

        if (bookingResult.Data.TotalAmount == 0)
        {
            var confirmResult = await _bookingService.ConfirmAsync(model.BookingId, "FREE-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(), cancellationToken);
            if (!confirmResult.IsSuccess)
            {
                if (isAjax)
                {
                    return Json(new { success = false, error = confirmResult.Error ?? "Failed to confirm free booking." });
                }
                TempData["PaymentError"] = confirmResult.Error ?? "Failed to confirm free booking.";
                return RedirectToAction(nameof(Checkout), new { id = model.BookingId });
            }

            if (isAjax)
            {
                return Json(new { success = true, checkoutUrl = Url.Action("Details", "Booking", new { id = model.BookingId }) });
            }
            TempData["SuccessMessage"] = "Booking confirmed successfully! Your tickets have been generated.";
            return RedirectToAction("Details", "Booking", new { id = model.BookingId });
        }

        var scheme = Request.Scheme;
        var host = Request.Host.Value;
        var baseUrl = $"{scheme}://{host}";

        var dto = new PaymentInitDto
        {
            BookingId = model.BookingId,
            Amount = model.Amount,
            Method = model.Method,
            SuccessUrl = $"{baseUrl}/Payment/Success",
            FailureUrl = $"{baseUrl}/Payment/Failed"
        };

        var result = await _paymentService.InitiateAsync(dto, cancellationToken);

        if (!result.IsSuccess)
        {
            if (isAjax)
            {
                return Json(new { success = false, error = result.Error });
            }
            TempData["PaymentError"] = result.Error;
            return RedirectToAction("Index", "Home");
        }

        // Redirect to Paymob Unified Checkout
        if (!string.IsNullOrEmpty(result.Data!.CheckoutUrl))
        {
            if (isAjax)
            {
                return Json(new { success = true, checkoutUrl = result.Data.CheckoutUrl });
            }
            return Redirect(result.Data.CheckoutUrl);
        }

        if (isAjax)
        {
            return Json(new { success = false, error = "Unable to generate checkout URL." });
        }
        TempData["PaymentError"] = "Unable to generate checkout URL.";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Handles the Paymob webhook callback (POST).
    /// This endpoint receives transaction status updates from Paymob.
    /// </summary>
    [HttpPost("api/payment/callback")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Callback(CancellationToken cancellationToken)
    {
        try
        {
            // Extract HMAC from query string
            var hmac = Request.Query["hmac"].ToString();

            // Extract callback data from query parameters
            var callbackData = new Dictionary<string, string>();
            foreach (var param in Request.Query)
            {
                if (param.Key != "hmac")
                {
                    callbackData[param.Key] = param.Value.ToString();
                }
            }

            // If data comes in form body, merge it
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                foreach (var field in form)
                {
                    callbackData[field.Key] = field.Value.ToString();
                }
            }
            else if (Request.ContentType != null && Request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(Request.Body);
                var bodyText = await reader.ReadToEndAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(bodyText))
                {
                    using var doc = JsonDocument.Parse(bodyText);
                    FlattenJsonElement(doc.RootElement, callbackData, "");
                }
            }

            // If HMAC not found in query string, extract it from the parsed body
            if (string.IsNullOrEmpty(hmac) && callbackData.TryGetValue("hmac", out var bodyHmac))
            {
                hmac = bodyHmac;
            }
            callbackData.Remove("hmac");

            var result = await _paymentService.HandlePaymobCallbackAsync(callbackData, hmac, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Paymob callback processing failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Paymob callback processed successfully for PaymentId: {PaymentId}",
                result.Data?.Id);

            return Ok(new { status = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Paymob callback");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles the redirect after a successful payment (GET - user redirect from Paymob).
    /// Extracts payment ID from URL path (/Payment/Success/pay-{paymentId}) if present,
    /// otherwise falls back to API-based transaction lookup.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Success(
        [FromQuery] string id,
        [FromQuery] bool success,
        [FromQuery] string hmac,
        CancellationToken cancellationToken)
    {
        var path = Request.Path.Value;
        var fullUrl = $"{Request.Scheme}://{Request.Host}{path}{Request.QueryString}";
        _logger.LogInformation("Paymob redirect callback triggered. FullUrl: {Url}, Id: {Id}, Success: {Success}", fullUrl, id, success);

        if (!success)
        {
            TempData["PaymentError"] = "Payment was not completed. Please try again.";
            return RedirectToAction("Index", "Home");
        }

        // Try to extract paymentId from URL path: /Payment/Success/pay-42
        var payPrefix = "/Payment/Success/pay-";
        if (path != null && path.StartsWith(payPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var payIdStr = path.Substring(payPrefix.Length);
            _logger.LogInformation("Extracted paymentId string from path: '{PayIdStr}'", payIdStr);
            if (int.TryParse(payIdStr, out var paymentId))
            {
                _logger.LogInformation("Attempting direct payment lookup by PaymentId: {PaymentId}", paymentId);
                var result = await _paymentService.HandleRedirectCallbackAsync(paymentId, id, hmac, cancellationToken);
                if (result.IsSuccess)
                {
                    TempData["PaymentSuccess"] = "Payment completed successfully! Your booking has been confirmed.";
                    return RedirectToAction("Index", "Home");
                }
                _logger.LogWarning("PaymentID lookup failed for PaymentId: {PaymentId}. Error: {Error}, falling back to API lookup.",
                    paymentId, result.Error);
            }
            else
            {
                _logger.LogWarning("Could not parse paymentId from path: '{PayIdStr}'", payIdStr);
            }
        }
        else
        {
            _logger.LogInformation("Path '{Path}' does not match prefix '{Prefix}'. Falling back to API lookup.", path, payPrefix);
        }

        // Fallback: use API-based transaction lookup
        if (!string.IsNullOrEmpty(id))
        {
            _logger.LogInformation("Falling back to API-based transaction lookup for TransactionId: {Id}", id);
            var callbackData = new Dictionary<string, string>();
            foreach (var param in Request.Query)
            {
                if (param.Key != "hmac")
                    callbackData[param.Key] = param.Value.ToString();
            }

            var result = await _paymentService.HandleRedirectCallbackAsync(id, callbackData, hmac, cancellationToken);
            if (result.IsSuccess)
            {
                TempData["PaymentSuccess"] = "Payment completed successfully! Your booking has been confirmed.";
                return RedirectToAction("Index", "Home");
            }

            TempData["PaymentError"] = result.Error ?? "Payment verification failed.";
            return RedirectToAction("Index", "Home");
        }

        _logger.LogWarning("No transaction ID available from query string.");
        TempData["PaymentError"] = "Payment verification failed.";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Handles the redirect after a failed payment.
    /// </summary>
    [HttpGet]
    public IActionResult Failed()
    {
        _logger.LogWarning("User payment failed redirect callback triggered.");
        TempData["PaymentError"] = "Payment was not completed. Please try again.";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// POST: Simulates sandbox/mock payment success in local development to bypass Paymob.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MockConfirm(int bookingId, CancellationToken cancellationToken)
    {
        var env = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
        bool isDev = env?.EnvironmentName == "Development" || Request.Host.Host == "localhost";
        if (!isDev)
        {
            TempData["PaymentError"] = "Mock payment is only allowed in local development.";
            return RedirectToAction("Checkout", new { id = bookingId });
        }

        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var booking = await _bookingService.GetBookingDetailAsync(bookingId, userId, cancellationToken);
        if (booking == null || !booking.IsSuccess || booking.Data == null)
        {
            TempData["PaymentError"] = "Booking not found.";
            return RedirectToAction("Index", "Home");
        }

        var result = await _bookingService.ConfirmAsync(bookingId, $"Mock_Dev_{Guid.NewGuid():N}", cancellationToken);
        if (result.IsSuccess)
        {
            TempData["PaymentSuccess"] = "Payment simulated successfully! Booking confirmed.";
            return RedirectToAction("Index", "Home");
        }

        TempData["PaymentError"] = result.Error ?? "Failed to simulate payment.";
        return RedirectToAction("Checkout", new { id = bookingId });
    }

    private static PaymentCheckoutViewModel ToCheckoutViewModel(BookingDetailDto dto) =>
        new()
        {
            Id = dto.Id,
            BookingReference = dto.BookingReference,
            EventTitle = dto.EventTitle,
            BookingDate = dto.BookingDate,
            Status = dto.Status,
            TotalAmount = dto.TotalAmount,
            ServiceFee = dto.ServiceFee,
            Items = dto.Items.Select(item => new PaymentCheckoutItemViewModel
            {
                Id = item.Id,
                TicketTypeId = item.TicketTypeId,
                TicketTypeName = item.TicketTypeName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

    /// <summary>
    /// Temporary recovery endpoint to manually confirm paid bookings that got expired/cancelled.
    /// </summary>
    [HttpGet("api/payment/recover-all")]
    [AllowAnonymous]
    public async Task<IActionResult> RecoverAll(CancellationToken cancellationToken)
    {
        var unitOfWork = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        
        // Find all cancelled bookings that have a pending payment record
        var payments = await unitOfWork.Payments.FindAsync(p => p.Status == PaymentStatus.Pending, cancellationToken);
        var results = new List<string>();

        foreach (var payment in payments)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(payment.BookingId, cancellationToken);
            if (booking != null && booking.Status == BookingStatus.Cancelled)
            {
                // Reset booking status to Pending
                booking.Status = BookingStatus.Pending;
                booking.CancellationReason = null;

                // Re-increment ticket type capacities
                var bookingItems = await unitOfWork.BookingItems.FindAsync(bi => bi.BookingId == booking.Id, cancellationToken);
                var eventWithTicketTypes = await unitOfWork.Events.GetByIdWithTicketTypesAsync(booking.EventId, cancellationToken);
                if (eventWithTicketTypes != null)
                {
                    foreach (var item in bookingItems)
                    {
                        var ticketType = eventWithTicketTypes.TicketTypes.FirstOrDefault(tt => tt.Id == item.TicketTypeId);
                        if (ticketType != null)
                        {
                            ticketType.SoldQuantity += item.Quantity;
                            unitOfWork.TicketTypes.Update(ticketType);
                        }
                    }
                }

                unitOfWork.Bookings.Update(booking);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Confirm the booking using the regular booking service
                var result = await _bookingService.ConfirmAsync(booking.Id, payment.TransactionId ?? $"Manual_{booking.Id}", cancellationToken);
                if (result.IsSuccess)
                {
                    results.Add($"Booking #{booking.Id} (Payment #{payment.Id}) successfully recovered.");
                }
                else
                {
                    results.Add($"Booking #{booking.Id} failed to confirm: {result.Error}");
                }
            }
        }

        if (!results.Any())
        {
            return Ok("No cancelled bookings with pending payments found to recover.");
        }

        return Ok(results);
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
