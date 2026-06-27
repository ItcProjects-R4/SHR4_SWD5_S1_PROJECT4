namespace EventifyPro.Web.Controllers;

[Authorize(Roles = RoleNames.Organizer)]
[TypeFilter(typeof(VerifiedOrganizerFilter))]
public class OrganizerWaitingListController : Controller
{
    private readonly IWaitingListService _waitingListService;
    private readonly IEventService _eventService;
    private readonly ITicketTypeService _ticketTypeService;

    public OrganizerWaitingListController(
        IWaitingListService waitingListService,
        IEventService eventService,
        ITicketTypeService ticketTypeService)
    {
        _waitingListService = waitingListService;
        _eventService = eventService;
        _ticketTypeService = ticketTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? eventId, 
        WaitingListStatus? status, 
        string? searchTerm, 
        int page = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        // Fetch the organizer's active events for dropdown filtering
        var eventsResult = await _eventService.GetOrganizerEventsAsync(organizerId, cancellationToken);
        var organizerEvents = (eventsResult.IsSuccess && eventsResult.Data != null) ? eventsResult.Data : new List<EventSummaryDto>();
        ViewBag.Events = organizerEvents.Select(e => new { e.Id, e.Title }).ToList();

        // Fetch waiting list statistics and entries page
        var summaryResult = await _waitingListService.GetOrganizerWaitingListSummaryAsync(
            eventId,
            status,
            searchTerm,
            organizerId,
            page,
            pageSize,
            cancellationToken);

        if (summaryResult.IsFailure || summaryResult.Data == null)
        {
            TempData["ErrorMessage"] = summaryResult.Error ?? "Failed to load waiting list.";
            return View(new OrganizerWaitingListListViewModel());
        }

        var summary = summaryResult.Data;

        // Fetch ticket types for selected event if any, to allow targeted "Notify Next" actions
        if (eventId.HasValue)
        {
            var ticketTypesResult = await _ticketTypeService.GetByEventAsync(eventId.Value, cancellationToken);
            var ticketTypes = (ticketTypesResult.IsSuccess && ticketTypesResult.Data != null) ? ticketTypesResult.Data : new List<TicketTypeResponseDto>();
            ViewBag.TicketTypes = ticketTypes.Select(tt => new { tt.Id, tt.Name }).ToList();
        }

        var entryViewModels = summary.Entries.Data.Select(w => 
        {
            var attendeeName = w.AttendeeName;
            var initials = string.Join("", attendeeName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(s => s[0])).ToUpper();
            if (string.IsNullOrEmpty(initials)) initials = "A";

            return new OrganizerWaitingListEntryViewModel
            {
                Id = w.Id,
                EventId = w.EventId,
                EventTitle = w.EventTitle,
                TicketTypeName = w.TicketTypeName,
                QuantityWanted = w.QuantityWanted,
                AttendeeName = attendeeName,
                AttendeeEmail = w.AttendeeEmail,
                AttendeeInitials = initials,
                Status = Enum.Parse<WaitingListStatus>(w.Status),
                JoinedAt = w.JoinedAt,
                NotifiedAt = w.NotifiedAt,
                ExpiresAt = w.ExpiresAt,
                PositionInQueue = w.PositionInQueue
            };
        }).ToList();

        var viewModel = new OrganizerWaitingListListViewModel
        {
            SelectedEventId = eventId,
            SelectedStatus = status,
            SearchTerm = searchTerm,
            TotalWaiting = summary.TotalWaiting,
            TotalNotified = summary.TotalNotified,
            TotalConverted = summary.TotalConverted,
            ConversionRate = summary.ConversionRate,
            Entries = entryViewModels,
            Page = page,
            PageSize = pageSize,
            TotalPages = summary.Entries.TotalPages
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotifyNext(int ticketTypeId, CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var ticketTypeResult = await _ticketTypeService.GetByIdAsync(ticketTypeId, cancellationToken);
        if (ticketTypeResult.IsFailure || ticketTypeResult.Data == null)
        {
            return NotFound("Ticket type not found.");
        }

        var eventResult = await _eventService.GetDetailAsync(ticketTypeResult.Data.EventId, cancellationToken);
        if (eventResult.IsFailure || eventResult.Data == null || eventResult.Data.OrganizerId != organizerId)
        {
            return Forbid();
        }

        var result = await _waitingListService.NotifyNextAsync(ticketTypeId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to notify the next user.";
        }
        else
        {
            TempData["SuccessMessage"] = "Successfully notified the next user in line.";
        }

        return RedirectToAction(nameof(Index), new { eventId = ticketTypeResult.Data.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceQueue(int eventId, CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var eventResult = await _eventService.GetDetailAsync(eventId, cancellationToken);
        if (eventResult.IsFailure || eventResult.Data == null || eventResult.Data.OrganizerId != organizerId)
        {
            return Forbid();
        }

        var result = await _waitingListService.AdvanceQueueAsync(eventId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to advance the queue.";
        }
        else
        {
            TempData["SuccessMessage"] = "Successfully advanced the waiting list queue.";
        }

        return RedirectToAction(nameof(Index), new { eventId = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _waitingListService.RemoveByOrganizerAsync(id, organizerId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to remove attendee from waiting list.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Attendee successfully removed from the waiting list.";
        return RedirectToAction(nameof(Index));
    }
}
