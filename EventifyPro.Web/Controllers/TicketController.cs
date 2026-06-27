using EventifyPro.Web.ViewModels.Ticket;


namespace EventifyPro.Web.Controllers;

[Authorize]
public class TicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly IPdfService _pdfService;
    private readonly IBookingService _bookingService;
    private readonly IQRService _qrService;

    public TicketController(
        ITicketService ticketService,
        IPdfService pdfService,
        IBookingService bookingService,
        IQRService qrService)
    {
        _ticketService = ticketService;
        _pdfService = pdfService;
        _bookingService = bookingService;
        _qrService = qrService;
    }

    /// <summary>
    /// Downloads the PDF for a specific ticket.
    /// Strictly verifies that the ticket belongs to the logged-in user.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Attendee)]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        // Verify that the ticket exists and belongs to the current user
        var ticketResult = await _ticketService.GetByIdAsync(id, userId, cancellationToken);
        if (ticketResult.IsFailure)
        {
            return Forbid();
        }

        try
        {
            var pdfBytes = await _pdfService.GenerateTicketPdfAsync(id, cancellationToken);
            return File(pdfBytes, "application/pdf", $"Ticket-{id}.pdf");
        }
        catch (Exception)
        {
            TempData["TicketError"] = "Unable to generate PDF ticket at this time.";
            return RedirectToAction(nameof(MyTickets));
        }
    }

    /// <summary>
    /// Serves a ticket QR code image after verifying ownership.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Attendee)]
    public async Task<IActionResult> QRCode(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var ticketResult = await _ticketService.GetByIdAsync(id, userId, cancellationToken);
        if (ticketResult.IsFailure || ticketResult.Data == null)
        {
            return Forbid();
        }

        try
        {
            var qrBytes = _qrService.GeneratePngBytes(ticketResult.Data.QRCode);
            return File(qrBytes, "image/png");
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Validates a scanned ticket QR code (AJAX).
    /// Accessible only by users with the Scanner role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Scanner)]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("scanner-limit")]
    public async Task<IActionResult> ValidateQR([FromBody] QRScanRequestDto dto, CancellationToken cancellationToken)
    {
        var scannerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(scannerId))
        {
            return Challenge();
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.QRCode) || dto.EventId <= 0)
        {
            return Json(new { success = false, error = "Invalid scan request parameters." });
        }

        var result = await _ticketService.ValidateAndUseAsync(dto, scannerId, cancellationToken);
        if (result.IsFailure)
        {
            return Json(new { success = false, error = result.Error });
        }

        return Json(new { success = true, data = result.Data });
    }

    /// <summary>
    /// Displays the list of tickets belonging to the logged-in user.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Attendee)]
    public async Task<IActionResult> MyTickets(
        string status = "active", 
        string search = "", 
        int page = 1, 
        int pageSize = 5, 
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await _ticketService.GetUserTicketsAsync(userId, status, search, page, pageSize, cancellationToken);
        if (result.IsFailure || result.Data == null)
        {
            TempData["TicketError"] = result.Error ?? "Failed to load tickets.";
            return View(new PaginatedTicketsViewModel());
        }

        var data = result.Data;

        var viewModels = data.Tickets.Data.Select(t => new TicketViewModel
        {
            Id = t.Id,
            EventId = t.EventId,
            BookingId = t.BookingId,
            TicketTypeId = t.TicketTypeId,
            TicketTypeName = t.TicketTypeName,
            QRCode = t.QRCode,
            IsUsed = t.IsUsed,
            UsedAt = t.UsedAt,
            CreatedAt = t.CreatedAt,
            EventTitle = t.EventTitle,
            EventDate = t.EventStartDate,
            EventEndDate = t.EventEndDate,
            BookingStatus = t.BookingStatus
        }).ToList();

        var viewModel = new PaginatedTicketsViewModel
        {
            Tickets = viewModels,
            CurrentPage = page,
            TotalPages = data.Tickets.TotalPages,
            CurrentStatus = status,
            SearchQuery = search,
            ActiveCount = data.ActiveCount,
            UsedCount = data.UsedCount,
            ExpiredCount = data.ExpiredCount,
            CancelledCount = data.CancelledCount
        };

        return View(viewModel);
    }
}
