
namespace EventifyPro.Web.Controllers
{
    [Route("Admin")]
    public class AdminEventsController : AdminBaseController
    {
        private readonly IAdminService _adminService;
        private readonly IEventService _eventService;
        private readonly ILogger<AdminEventsController> _logger;

        public AdminEventsController(
            UserManager<ApplicationUser> userManager,
            IAdminService adminService,
            IEventService eventService,
            ILogger<AdminEventsController> logger) : base(userManager)
        {
            _adminService = adminService;
            _eventService = eventService;
            _logger = logger;
        }

        [HttpGet("Events")]
        public async Task<IActionResult> Events(string searchTerm, string statusFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var result = await _adminService.GetEventsPageAsync(searchTerm, statusFilter, page, cancellationToken);
                if (result.IsFailure || result.Data == null)
                {
                    TempData["AdminError"] = result.Error ?? "Error loading events.";
                    return View("~/Views/Admin/Events.cshtml", new List<Event>());
                }

                var data = result.Data;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.StatusFilter = statusFilter;
                ViewBag.PageNumber = page ?? 1;
                ViewBag.TotalPages = data.TotalPages;
                ViewBag.TotalCount = data.TotalCount;

                return View("~/Views/Admin/Events.cshtml", data.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading events");
                TempData["AdminError"] = "Error loading events.";
                return View("~/Views/Admin/Events.cshtml", new List<Event>());
            }
        }

        [HttpPost("DisableEvent")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> DisableEvent(int eventId, string reason, string searchTerm, string statusFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["AdminError"] = "A cancellation reason is required.";
                    return RedirectToAction(nameof(Events), new { searchTerm, statusFilter, page });
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(adminId))
                {
                    return Challenge();
                }

                var result = await _eventService.CancelByAdminAsync(eventId, adminId, reason, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Failed to cancel the event.";
                }
                else
                {
                    TempData["AdminSuccess"] = "Event has been cancelled and attendees/organizer notified.";
                }

                return RedirectToAction(nameof(Events), new { searchTerm, statusFilter, page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling event");
                TempData["AdminError"] = "Error cancelling event.";
                return RedirectToAction(nameof(Events), new { searchTerm, statusFilter, page });
            }
        }

        [HttpPost("ApproveEvent")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> ApproveEvent(int id, string searchTerm, string statusFilter, int? page, CancellationToken cancellationToken)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Challenge();
            }

            var result = await _eventService.ApproveAsync(id, adminId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["AdminError"] = result.Error ?? "Failed to approve event.";
            }
            else
            {
                TempData["AdminSuccess"] = "Event approved and published successfully.";
            }

            if (string.IsNullOrEmpty(searchTerm) && string.IsNullOrEmpty(statusFilter) && !page.HasValue)
            {
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToAction("Index", "Admin");
            }
            return RedirectToAction(nameof(Events), new { searchTerm, statusFilter, page });
        }

        [HttpPost("RejectEvent")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> RejectEvent(int id, string reason, string searchTerm, string statusFilter, int? page, CancellationToken cancellationToken)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["AdminError"] = "A rejection reason is required.";
                if (string.IsNullOrEmpty(searchTerm) && string.IsNullOrEmpty(statusFilter) && !page.HasValue)
                {
                    var refererUrl = Request.Headers["Referer"].ToString();
                    if (!string.IsNullOrEmpty(refererUrl))
                    {
                        return Redirect(refererUrl);
                    }
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction(nameof(Events), new { searchTerm, statusFilter, page });
            }

            var result = await _eventService.RejectAsync(id, adminId, reason, cancellationToken);
            if (result.IsFailure)
            {
                TempData["AdminError"] = result.Error ?? "Failed to reject event.";
            }
            else
            {
                TempData["AdminSuccess"] = "Event rejected and organizer notified.";
            }

            if (string.IsNullOrEmpty(searchTerm) && string.IsNullOrEmpty(statusFilter) && !page.HasValue)
            {
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToAction("Index", "Admin");
            }
            return RedirectToAction(nameof(Events), new { searchTerm, statusFilter, page });
        }
    }
}
