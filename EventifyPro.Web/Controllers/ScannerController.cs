

namespace EventifyPro.Web.Controllers;

[Authorize]
public class ScannerController : Controller
{
    private readonly IScanLogService _scanLogService;
    private readonly IUnitOfWork _unitOfWork;

    public ScannerController(IScanLogService scanLogService, IUnitOfWork unitOfWork)
    {
        _scanLogService = scanLogService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Serves the Scanner welcome/overview page.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Scanner)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var scannerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(scannerId))
        {
            return Challenge();
        }

        var scanner = await _unitOfWork.Users.GetByIdAsync(scannerId, cancellationToken);
        if (scanner == null || !scanner.IsActive || string.IsNullOrWhiteSpace(scanner.ScannerCreatedByOrganizerId))
        {
            return Forbid();
        }

        var events = await _unitOfWork.DbContext.Set<EventScanner>()
            .Where(es => es.ScannerId == scannerId && !es.Event.IsDeleted && es.Event.Status != EventStatus.Cancelled)
            .Select(es => es.Event)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);

        ViewBag.Events = events;
        return View();
    }

    /// <summary>
    /// Displays the session scan logs for the logged-in scanner.
    /// Accessible only by the Scanner role.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Scanner)]
    public async Task<IActionResult> SessionLogs(int eventId, DateTime sessionStart, int page = 1, CancellationToken cancellationToken = default)
    {
        var scannerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(scannerId))
        {
            return Challenge();
        }

        int pageSize = 10;
        var result = await _scanLogService.GetSessionLogsAsync(scannerId, eventId, sessionStart.UserInputToUtc(), page, pageSize, cancellationToken);
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        var eventTitle = eventEntity?.Title ?? "Unknown Event";

        if (result.IsFailure)
        {
            TempData["ScannerError"] = result.Error ?? "Failed to load session logs.";
            if (result.Error != null && result.Error.Contains("authorized"))
            {
                return Forbid();
            }
            return View(new SessionLogsViewModel
            {
                EventId = eventId,
                EventTitle = eventTitle,
                SessionStart = sessionStart,
                Logs = Eventify.Shared.Wrappers.PagedResult<BLL.DTOs.Scanner.ScanLogResponseDto>.Empty(page, pageSize)
            });
        }

        return View(new SessionLogsViewModel
        {
            EventId = eventId,
            EventTitle = eventTitle,
            SessionStart = sessionStart,
            Logs = result.Data!
        });
    }

    /// <summary>
    /// Displays all scan logs for a specific event.
    /// Accessible only by Organizer and Admin roles.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Organizer},{RoleNames.Admin}")]
    public async Task<IActionResult> EventLogs(int eventId, int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }
        var isAdmin = User.IsInRole(RoleNames.Admin);

        int pageSize = 15;
        var result = await _scanLogService.GetEventLogsAsync(eventId, userId, isAdmin, page, pageSize, cancellationToken);
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        var eventTitle = eventEntity?.Title ?? "Unknown Event";

        if (result.IsFailure)
        {
            TempData["ScannerError"] = result.Error ?? "Failed to load event logs.";
            if (result.Error != null && result.Error.Contains("authorized"))
            {
                return Forbid();
            }
            return View(new EventLogsViewModel
            {
                EventId = eventId,
                EventTitle = eventTitle,
                Logs = Eventify.Shared.Wrappers.PagedResult<BLL.DTOs.Scanner.ScanLogResponseDto>.Empty(page, pageSize)
            });
        }

        return View(new EventLogsViewModel
        {
            EventId = eventId,
            EventTitle = eventTitle,
            Logs = result.Data!
        });
    }
}
