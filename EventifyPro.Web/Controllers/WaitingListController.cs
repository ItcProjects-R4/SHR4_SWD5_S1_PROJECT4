

namespace EventifyPro.Web.Controllers;

[Authorize]
public class WaitingListController : Controller
{
    private readonly IWaitingListService _waitingListService;
    private readonly ITicketTypeService _ticketTypeService;
    private readonly ILogger<WaitingListController> _logger;

    public WaitingListController(
        IWaitingListService waitingListService,
        ITicketTypeService ticketTypeService,
        ILogger<WaitingListController> logger)
    {
        _waitingListService = waitingListService;
        _ticketTypeService = ticketTypeService;
        _logger = logger;
    }

    /// <summary>
    /// GET: Displays the user's waiting list entries.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            var result = await _waitingListService.GetUserWaitingListEntriesAsync(userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["WaitingListError"] = result.Error ?? "Failed to load waiting list.";
                return View(new List<Eventify.Domain.Entities.WaitingList>());
            }

            var entries = result.Data.Select(dto => new Eventify.Domain.Entities.WaitingList
            {
                Id = dto.Id,
                UserId = dto.UserId,
                EventId = dto.EventId,
                Event = new Eventify.Domain.Entities.Event
                {
                    Id = dto.EventId,
                    Title = dto.EventTitle
                },
                TicketTypeId = dto.TicketTypeId,
                TicketType = new Eventify.Domain.Entities.TicketType
                {
                    Id = dto.TicketTypeId,
                    Name = dto.TicketTypeName
                },
                QuantityWanted = dto.QuantityWanted,
                Status = Enum.Parse<Eventify.Domain.Enums.WaitingListStatus>(dto.Status),
                JoinedAt = dto.CreatedAt
            }).ToList();

            return View(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving waiting list for user {UserId}", userId);
            TempData["WaitingListError"] = "An unexpected error occurred while loading the waiting list.";
            return View(new List<Eventify.Domain.Entities.WaitingList>());
        }
    }

    /// <summary>
    /// POST: Adds a user to the waiting list for a specific ticket type.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int ticketTypeId, int quantityWanted = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            var ticketTypeResult = await _ticketTypeService.GetByIdAsync(ticketTypeId, cancellationToken);
            if (!ticketTypeResult.IsSuccess || ticketTypeResult.Data == null)
            {
                TempData["WaitingListError"] = ticketTypeResult.Error ?? "Ticket type not found.";
                return RedirectToAction(nameof(Index));
            }

            var ticketType = ticketTypeResult.Data;

            var dto = new WaitingListJoinDto 
            { 
                EventId = ticketType.EventId,
                TicketTypeId = ticketTypeId,
                QuantityWanted = quantityWanted > 0 ? quantityWanted : 1
            };

            var result = await _waitingListService.JoinAsync(dto, userId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["WaitingListError"] = result.Error ?? "Failed to join waiting list.";
                return RedirectToAction(nameof(Index));
            }

            TempData["WaitingListSuccess"] = "You have been added to the waiting list. You'll be notified when tickets become available.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining waiting list for user {UserId} and ticket type {TicketTypeId}", userId, ticketTypeId);
            TempData["WaitingListError"] = "An unexpected error occurred while joining the waiting list.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// POST: Removes a user from the waiting list.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int waitingListEntryId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            var result = await _waitingListService.LeaveAsync(waitingListEntryId, userId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["WaitingListError"] = result.Error ?? "Failed to leave waiting list.";
                return RedirectToAction(nameof(Index));
            }

            TempData["WaitingListSuccess"] = "You have been removed from the waiting list.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from waiting list for user {UserId} and entry {EntryId}", userId, waitingListEntryId);
            TempData["WaitingListError"] = "An unexpected error occurred while leaving the waiting list.";
            return RedirectToAction(nameof(Index));
        }
    }
}
