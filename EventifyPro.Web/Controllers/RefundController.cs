namespace EventifyPro.Web.Controllers;

[Authorize]
public class RefundController : Controller
{
    private readonly IRefundService _refundService;
    private readonly ILogger<RefundController> _logger;

    public RefundController(
        IRefundService refundService,
        ILogger<RefundController> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// GET: Displays refunds for a specific booking.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ByBooking(int bookingId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            var result = await _refundService.GetByBookingAsync(bookingId, userId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["RefundError"] = result.Error ?? "Failed to load refund information.";
                return View(Array.Empty<object>());
            }

            return View(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refunds for booking {BookingId} and user {UserId}", bookingId, userId);
            TempData["RefundError"] = "An unexpected error occurred while loading refund information.";
            return View(Array.Empty<object>());
        }
    }

    /// <summary>
    /// GET: Displays the refund initiation form for a specific booking.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Initiate(int bookingId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await _refundService.GetRefundInitiationDetailsAsync(bookingId, userId, cancellationToken);
        if (result.IsFailure || result.Data == null)
        {
            TempData["RefundError"] = result.Error ?? "Failed to initiate refund request.";
            return RedirectToAction("Details", "Booking", new { id = bookingId });
        }

        var details = result.Data;

        var model = new RefundInitiateViewModel
        {
            BookingId = bookingId,
            PaymentId = details.PaymentId,
            Amount = details.MaxRefundableAmount,
            MaxRefundableAmount = details.MaxRefundableAmount
        };

        return View(model);
    }

    /// <summary>
    /// POST: Submits a refund request for a booking.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Initiate(RefundInitiateViewModel model, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Length < 10)
        {
            TempData["RefundError"] = "Please provide a reason for the refund (at least 10 characters).";
            return View(model);
        }

        if (model.Amount <= 0)
        {
            TempData["RefundError"] = "Refund amount must be greater than zero.";
            return View(model);
        }

        try
        {
            var dto = new RefundCreateDto 
            { 
                PaymentId = model.PaymentId,
                Amount = model.Amount,
                Reason = model.Reason
            };
            var result = await _refundService.InitiateAsync(dto, userId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["RefundError"] = result.Error ?? "Failed to process refund request.";
                return View(model);
            }

            TempData["RefundSuccess"] = "Your refund request has been processed successfully.";
            return RedirectToAction(nameof(ByBooking), new { bookingId = model.BookingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating refund for payment {PaymentId} and user {UserId}", model.PaymentId, userId);
            TempData["RefundError"] = "An unexpected error occurred while processing your refund request.";
            return View(model);
        }
    }

    /// <summary>
    /// GET: Gets the total refunded amount for a payment.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTotalRefunded(int paymentId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _refundService.GetTotalRefundedAsync(paymentId, cancellationToken);
            if (result.IsFailure)
            {
                return Json(new { success = false, error = result.Error ?? "Failed to retrieve refund total." });
            }

            return Json(new { success = true, totalRefunded = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving total refunded amount for payment {PaymentId}", paymentId);
            return Json(new { success = false, error = "An unexpected error occurred." });
        }
    }
}

/// <summary>
/// ViewModel for refund initiation form.
/// </summary>
public class RefundInitiateViewModel
{
    public int BookingId { get; set; }
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal MaxRefundableAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}
