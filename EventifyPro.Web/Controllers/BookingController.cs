
namespace EventifyPro.Web.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IBookingService bookingService,
        IEventService eventService,
        ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// GET: Displays detailed information about a specific booking.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await _bookingService.GetBookingDetailAsync(id, userId, cancellationToken);
        if (result.IsFailure || result.Data == null)
        {
            TempData["BookingError"] = result.Error ?? "Booking not found.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = result.Data.Adapt<BookingDetailViewModel>();
        return View(viewModel);
    }

    /// <summary>
    /// GET: Serves the booking creation page for a specific event.
    /// </summary>
    [HttpGet("/Booking/Create/{eventId}")]
    [Authorize(Roles = RoleNames.Attendee)]
    public async Task<IActionResult> Create(int eventId, int? waitingListId = null)
    {
        var eventResult = await _eventService.GetDetailAsync(eventId);

        if (eventResult.IsFailure || eventResult.Data == null)
        {
            return NotFound("Event not found.");
        }

        var eventEntity = eventResult.Data;

        if (eventEntity.Status != EventStatus.Published.ToString())
        {
            TempData["BookingError"] = "Cannot book tickets for an unpublished event.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        if (eventEntity.StartDate <= DateTime.UtcNow)
        {
            TempData["BookingError"] = "Cannot book tickets for an event that has already started or passed.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var model = new BookingCreateViewModel
        {
            EventId = eventId,
            Items = new List<BookingItemViewModel>(),
            WaitingListId = waitingListId
        };
        return View(model);
    }

    /// <summary>
    /// POST: Creates a new pending booking.
    /// Redirects to payment checkout on success.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Attendee)]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("booking-limit")]
    public async Task<IActionResult> Create(BookingCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var dto = new BookingCreateDto
        {
            EventId = model.EventId,
            Items = model.Items.Select(i => new BookingItemRequestDto
            {
                TicketTypeId = i.TicketTypeId,
                Quantity = i.Quantity
            }).ToList(),
            WaitingListId = model.WaitingListId
        };

        var result = await _bookingService.CreatePendingAsync(dto, userId, cancellationToken);
        if (result.IsFailure || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create booking.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Booking created successfully. Please complete your payment.";
        return RedirectToAction("Checkout", "Payment", new { id = result.Data.Id });
    }

    /// <summary>
    /// POST: Confirms a booking manually (typically invoked by admin/mock tools).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, string transactionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            TempData["BookingError"] = "Transaction ID is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _bookingService.ConfirmAsync(id, transactionId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["BookingError"] = result.Error ?? "Failed to confirm booking.";
        }
        else
        {
            TempData["SuccessMessage"] = "Booking confirmed successfully.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// POST: Cancels a booking.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Attendee)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["BookingError"] = "Cancellation reason is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _bookingService.CancelAsync(id, userId, reason, cancellationToken);
        if (result.IsFailure)
        {
            TempData["BookingError"] = result.Error ?? "Failed to cancel booking.";
        }
        else
        {
            TempData["SuccessMessage"] = "Booking cancelled successfully.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// GET: Displays a list of all bookings for the logged-in user.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Attendee)]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 12, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 12;

        var pagedResult = await _bookingService.GetUserBookingsAsync(userId, pageNumber, pageSize, cancellationToken);
        if (pagedResult == null)
        {
            TempData["BookingError"] = "Failed to load bookings.";
            return View(new List<BookingSummaryViewModel>());
        }

        var viewModels = pagedResult.Data.Select(b => new BookingSummaryViewModel
        {
            Id = b.Id,
            UserId = b.UserId,
            EventId = b.EventId,
            EventTitle = b.EventTitle,
            TotalAmount = b.TotalAmount,
            Status = b.Status,
            BookingReference = b.BookingReference,
            BookingDate = b.BookingDate
        }).ToList();

        // Pass the list to the view. (We can pass pagination metadata in ViewBag if needed)
        ViewBag.TotalCount = pagedResult.TotalCount;
        ViewBag.PageNumber = pagedResult.PageNumber;
        ViewBag.PageSize = pagedResult.PageSize;
        ViewBag.TotalPages = pagedResult.TotalPages;

        return View(viewModels);
    }

    /// <summary>
    /// GET: Helper action to view tickets (redirects to TicketController).
    /// </summary>
    [HttpGet]
    public IActionResult MyTickets()
    {
        return RedirectToAction("MyTickets", "Ticket");
    }
}
