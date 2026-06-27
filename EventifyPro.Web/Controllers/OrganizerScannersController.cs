
namespace EventifyPro.Controllers;

[Authorize(Roles = RoleNames.Organizer)]
[TypeFilter(typeof(VerifiedOrganizerFilter))]
public class OrganizerScannersController : Controller
{
    private readonly IOrganizerScannersService _scannersService;
    private readonly IEventService _eventService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrganizerScannersController(
        IOrganizerScannersService scannersService,
        IEventService eventService,
        UserManager<ApplicationUser> userManager)
    {
        _scannersService = scannersService;
        _eventService = eventService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _scannersService.GetScannersListAsync(organizerId, searchTerm, page, pageSize, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to retrieve scanners list.";
            return RedirectToAction("Index", "Dashboard");
        }

        var data = result.Data;

        var viewModels = data.Data.Select(s => new ScannerViewModel
        {
            Id = s.Id,
            FullName = s.FullName,
            Email = s.Email,
            CreatedAt = s.CreatedAt,
            IsActive = s.IsActive,
            TotalScans = s.TotalScans,
            LastScannedEventTitle = s.LastScannedEventTitle,
            LastScannedAt = s.LastScannedAt,
            LastScanStatus = s.LastScanStatus
        }).ToList();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = data.TotalPages;

        return View(viewModels);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateScannerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScannerViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _scannersService.CreateScannerAccountAsync(new CreateScannerDto
        {
            FullName = model.FullName,
            Email = model.Email,
            Password = model.Password
        }, organizerId, cancellationToken);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create scanner account.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Scanner account created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Scanner ID is required.");
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _scannersService.GetScannerDetailsAsync(id, organizerId, 1, 1, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            return NotFound("Scanner not found.");
        }

        var scanner = result.Data;

        var model = new EditScannerViewModel
        {
            Id = scanner.Id,
            FullName = scanner.FullName,
            Email = scanner.Email
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditScannerViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _scannersService.UpdateScannerAsync(model.Id, model.FullName, model.NewPassword, organizerId, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update scanner account.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Scanner account updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Scanner ID is required.");
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _scannersService.ToggleScannerActiveStatusAsync(id, organizerId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to toggle status.";
        }
        else
        {
            TempData["SuccessMessage"] = "Scanner access status toggled successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Scanner ID is required.");
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _scannersService.GetScannerDetailsAsync(id, organizerId, page, pageSize, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            return NotFound("Scanner not found.");
        }

        var scanner = result.Data;

        var logsList = scanner.ScanLogs.Data.Select(s => new ScanLogItemViewModel
        {
            Id = s.Id,
            EventTitle = s.EventTitle ?? "N/A",
            EventId = s.EventId,
            TicketSerialNumber = s.TicketId.HasValue ? "TCK-" + s.TicketId.Value : null,
            AttendeeName = s.AttendeeName,
            ScannedAt = s.ScannedAt,
            Result = s.ScanResult,
            Notes = s.Notes
        }).ToList();

        var model = new ScannerDetailsViewModel
        {
            Id = scanner.Id,
            FullName = scanner.FullName,
            Email = scanner.Email,
            IsActive = scanner.IsActive,
            CreatedAt = scanner.CreatedAt,
            TotalScans = scanner.TotalScans,
            ValidScans = scanner.ValidScans,
            InvalidScans = scanner.InvalidScans,
            ScanLogs = logsList,
            Page = page,
            PageSize = pageSize,
            TotalPages = scanner.ScanLogs.TotalPages
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignEvents(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Scanner ID is required.");
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var scanner = await _userManager.FindByIdAsync(id);
        if (scanner == null || scanner.ScannerCreatedByOrganizerId != organizerId)
        {
            return NotFound("Scanner not found.");
        }

        var eventsResult = await _eventService.GetOrganizerEventsAsync(organizerId, cancellationToken);
        if (!eventsResult.IsSuccess || eventsResult.Data == null)
        {
            TempData["ErrorMessage"] = eventsResult.Error ?? "Failed to retrieve events.";
            return RedirectToAction(nameof(Index));
        }

        var events = eventsResult.Data
            .Where(e => e.Status != EventStatus.Cancelled.ToString())
            .Select(dto => new Eventify.Domain.Entities.Event
            {
                Id = dto.Id,
                Title = dto.Title,
                StartDate = dto.StartDate,
                City = dto.City,
                Status = Enum.Parse<EventStatus>(dto.Status)
            }).ToList();

        var assignmentsResult = await _scannersService.GetScannerAssignmentsAsync(id, organizerId, cancellationToken);
        var assignedEventIds = assignmentsResult.IsSuccess && assignmentsResult.Data != null
            ? assignmentsResult.Data.Where(a => a.IsAssigned).Select(a => a.EventId).ToList()
            : new List<int>();

        ViewBag.Scanner = scanner;
        ViewBag.AssignedEventIds = assignedEventIds;

        return View(events);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEvents(string id, int[] selectedEventIds, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Scanner ID is required.");
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var scanner = await _userManager.FindByIdAsync(id);
        if (scanner == null || scanner.ScannerCreatedByOrganizerId != organizerId)
        {
            return NotFound("Scanner not found.");
        }

        var filteredEventIds = selectedEventIds?.ToList() ?? new List<int>();

        var result = await _scannersService.AssignScannerToEventsAsync(id, filteredEventIds, organizerId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to update assignments.";
        }
        else
        {
            TempData["SuccessMessage"] = "Scanner event assignments updated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
