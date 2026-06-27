using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EventifyPro.BLL.Services.Interfaces;
using EventifyPro.BLL.DTOs.TicketType;

using Mapster;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using EventifyPro.Web.Filters;
using System;

namespace EventifyPro.Web.Controllers
{
    [Authorize(Roles = RoleNames.Organizer)]
    [TypeFilter(typeof(VerifiedOrganizerFilter))]
    public class TicketTypesController : Controller
    {
        private readonly ITicketTypeService _ticketTypeService;
        private readonly IEventService _eventService;

        public TicketTypesController(ITicketTypeService ticketTypeService, IEventService eventService)
        {
            _ticketTypeService = ticketTypeService;
            _eventService = eventService;
        }

        // GET: TicketTypes?eventId=5
        [HttpGet]
        public async Task<IActionResult> Index(int eventId, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var eventResult = await _eventService.GetDetailAsync(eventId, cancellationToken);
            if (!eventResult.IsSuccess || eventResult.Data == null)
            {
                return NotFound("Event not found.");
            }

            var eventEntity = eventResult.Data;
            if (eventEntity.OrganizerId != organizerId)
            {
                return Forbid();
            }

            var ticketTypesResult = await _ticketTypeService.GetByEventAsync(eventId, cancellationToken);
            if (!ticketTypesResult.IsSuccess || ticketTypesResult.Data == null)
            {
                TempData["ErrorMessage"] = ticketTypesResult.Error ?? "Failed to retrieve ticket types.";
                return View(new List<TicketTypeViewModel>());
            }

            ViewBag.EventId = eventId;
            ViewBag.EventTitle = eventEntity.Title;

            var viewModels = ticketTypesResult.Data.Adapt<List<TicketTypeViewModel>>();
            return View(viewModels);
        }

        // GET: TicketTypes/Create?eventId=5
        [HttpGet]
        public async Task<IActionResult> Create(int eventId, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var eventResult = await _eventService.GetDetailAsync(eventId, cancellationToken);
            if (!eventResult.IsSuccess || eventResult.Data == null)
            {
                return NotFound("Event not found.");
            }

            var eventEntity = eventResult.Data;
            if (eventEntity.OrganizerId != organizerId)
            {
                return Forbid();
            }

            ViewBag.EventTitle = eventEntity.Title;
            return View(new TicketTypeFormViewModel { EventId = eventId });
        }

        // POST: TicketTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketTypeFormViewModel model, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var eventResult = await _eventService.GetDetailAsync(model.EventId, cancellationToken);
            if (!eventResult.IsSuccess || eventResult.Data == null)
            {
                return NotFound("Event not found.");
            }

            var eventEntity = eventResult.Data;
            if (eventEntity.OrganizerId != organizerId)
            {
                return Forbid();
            }

            // Ticket price validation
            if (model.Price < 0)
            {
                ModelState.AddModelError(nameof(model.Price), "Ticket price cannot be negative.");
            }

            // Ticket Sale dates validation
            if (model.SaleStartDate.HasValue && model.SaleStartDate.Value.UserInputToUtc() > eventEntity.EndDate)
            {
                ModelState.AddModelError(nameof(model.SaleStartDate), "Ticket sale start date cannot be after the event ends.");
            }
            if (model.SaleEndDate.HasValue && model.SaleEndDate.Value.UserInputToUtc() > eventEntity.EndDate)
            {
                ModelState.AddModelError(nameof(model.SaleEndDate), "Ticket sale end date cannot be after the event ends.");
            }
            if (model.SaleStartDate.HasValue && model.SaleEndDate.HasValue && model.SaleEndDate.Value <= model.SaleStartDate.Value)
            {
                ModelState.AddModelError(nameof(model.SaleEndDate), "Ticket sale end date must be after the sale start date.");
            }

            // Event MaxCapacity vs Ticket Quantities validation
            var existingQuantity = await _ticketTypeService.GetSumOfTotalQuantityByEventAsync(model.EventId, null, cancellationToken);

            if (eventEntity.MaxCapacity.HasValue && (existingQuantity + model.TotalQuantity) > eventEntity.MaxCapacity.Value)
            {
                ModelState.AddModelError(nameof(model.TotalQuantity), $"The total quantity of all ticket types (currently {existingQuantity + model.TotalQuantity}) cannot exceed the event's maximum capacity of {eventEntity.MaxCapacity.Value}.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.EventTitle = eventEntity.Title;
                return View(model);
            }

            var dto = new TicketTypeCreateDto
            {
                EventId = model.EventId,
                Name = System.Net.WebUtility.HtmlEncode(model.Name?.Trim() ?? string.Empty),
                Price = model.Price,
                TotalQuantity = model.TotalQuantity,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : System.Net.WebUtility.HtmlEncode(model.Description.Trim()),
                SaleStartDate = model.SaleStartDate.UserInputToUtc(),
                SaleEndDate = model.SaleEndDate.UserInputToUtc()
            };

            var result = await _ticketTypeService.AddToEventAsync(model.EventId, dto, organizerId, cancellationToken);
            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create ticket type.");
                ViewBag.EventTitle = eventEntity.Title;
                return View(model);
            }

            TempData["SuccessMessage"] = "Ticket type created successfully.";
            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }

        // GET: TicketTypes/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var ticketTypeResult = await _ticketTypeService.GetByIdAsync(id, cancellationToken);
            if (!ticketTypeResult.IsSuccess || ticketTypeResult.Data == null)
            {
                return NotFound("Ticket type not found.");
            }

            var ticketType = ticketTypeResult.Data;

            var eventResult = await _eventService.GetDetailAsync(ticketType.EventId, cancellationToken);
            if (!eventResult.IsSuccess || eventResult.Data == null || eventResult.Data.OrganizerId != organizerId)
            {
                return Forbid();
            }

            var eventEntity = eventResult.Data;

            ViewBag.EventTitle = eventEntity.Title;
            ViewBag.SoldQuantity = ticketType.SoldQuantity;

            var model = new TicketTypeFormViewModel
            {
                Id = ticketType.Id,
                EventId = ticketType.EventId,
                Name = ticketType.Name,
                Price = ticketType.Price,
                TotalQuantity = ticketType.TotalQuantity,
                Description = ticketType.Description,
                SaleStartDate = ticketType.SaleStartDate?.ToEgyptTime(),
                SaleEndDate = ticketType.SaleEndDate?.ToEgyptTime()
            };

            return View(model);
        }

        // POST: TicketTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TicketTypeFormViewModel model, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            if (id != model.Id)
            {
                return BadRequest("ID mismatch.");
            }

            var ticketTypeResult = await _ticketTypeService.GetByIdAsync(id, cancellationToken);
            if (!ticketTypeResult.IsSuccess || ticketTypeResult.Data == null)
            {
                return NotFound("Ticket type not found.");
            }

            var ticketType = ticketTypeResult.Data;

            var eventResult = await _eventService.GetDetailAsync(ticketType.EventId, cancellationToken);
            if (!eventResult.IsSuccess || eventResult.Data == null || eventResult.Data.OrganizerId != organizerId)
            {
                return Forbid();
            }

            var eventEntity = eventResult.Data;

            // Ticket price validation
            if (model.Price < 0)
            {
                ModelState.AddModelError(nameof(model.Price), "Ticket price cannot be negative.");
            }

            // Ticket Sale dates validation
            if (model.SaleStartDate.HasValue && model.SaleStartDate.Value.UserInputToUtc() > eventEntity.EndDate)
            {
                ModelState.AddModelError(nameof(model.SaleStartDate), "Ticket sale start date cannot be after the event ends.");
            }
            if (model.SaleEndDate.HasValue && model.SaleEndDate.Value.UserInputToUtc() > eventEntity.EndDate)
            {
                ModelState.AddModelError(nameof(model.SaleEndDate), "Ticket sale end date cannot be after the event ends.");
            }
            if (model.SaleStartDate.HasValue && model.SaleEndDate.HasValue && model.SaleEndDate.Value <= model.SaleStartDate.Value)
            {
                ModelState.AddModelError(nameof(model.SaleEndDate), "Ticket sale end date must be after the sale start date.");
            }

            // Event MaxCapacity vs Ticket Quantities validation
            var otherQuantity = await _ticketTypeService.GetSumOfTotalQuantityByEventAsync(ticketType.EventId, model.Id, cancellationToken);

            if (eventEntity.MaxCapacity.HasValue && (otherQuantity + model.TotalQuantity) > eventEntity.MaxCapacity.Value)
            {
                ModelState.AddModelError(nameof(model.TotalQuantity), $"The total quantity of all ticket types (currently {otherQuantity + model.TotalQuantity}) cannot exceed the event's maximum capacity of {eventEntity.MaxCapacity.Value}.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.EventTitle = eventEntity.Title;
                ViewBag.SoldQuantity = ticketType.SoldQuantity;
                return View(model);
            }

            var dto = new TicketTypeUpdateDto
            {
                Id = model.Id,
                Name = System.Net.WebUtility.HtmlEncode(model.Name?.Trim() ?? string.Empty),
                Price = model.Price,
                TotalQuantity = model.TotalQuantity,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : System.Net.WebUtility.HtmlEncode(model.Description.Trim()),
                SaleStartDate = model.SaleStartDate.UserInputToUtc(),
                SaleEndDate = model.SaleEndDate.UserInputToUtc()
            };

            var result = await _ticketTypeService.UpdateAsync(id, dto, organizerId, cancellationToken);
            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update ticket type.");
                ViewBag.EventTitle = eventEntity.Title;
                ViewBag.SoldQuantity = ticketType.SoldQuantity;
                return View(model);
            }

            TempData["SuccessMessage"] = "Ticket type updated successfully.";
            return RedirectToAction(nameof(Index), new { eventId = ticketType.EventId });
        }

        // GET: TicketTypes/CheckDuplicateName
        [HttpGet]
        public async Task<IActionResult> CheckDuplicateName(string name, int eventId, int? id, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var eventResult = await _eventService.GetDetailAsync(eventId, cancellationToken);
            if (!eventResult.IsSuccess || eventResult.Data == null || eventResult.Data.OrganizerId != organizerId)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(true);
            }

            var exists = await _ticketTypeService.ExistsByNameAsync(name, eventId, id, cancellationToken);
            return Json(!exists);
        }

        // POST: TicketTypes/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Delete(int id, int eventId, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var ticketTypeResult = await _ticketTypeService.GetByIdAsync(id, cancellationToken);
            if (!ticketTypeResult.IsSuccess || ticketTypeResult.Data == null)
            {
                return NotFound("Ticket type not found.");
            }

            var ticketType = ticketTypeResult.Data;
            if (ticketType.EventId != eventId)
            {
                return BadRequest("Event ID mismatch.");
            }

            var eventResult = await _eventService.GetDetailAsync(eventId, cancellationToken);
            if (!eventResult.IsSuccess || eventResult.Data == null || eventResult.Data.OrganizerId != organizerId)
            {
                return Forbid();
            }

            var result = await _ticketTypeService.DeleteAsync(id, organizerId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to delete ticket type.";
                return RedirectToAction(nameof(Index), new { eventId = eventId });
            }

            TempData["SuccessMessage"] = "Ticket type deleted successfully.";
            return RedirectToAction(nameof(Index), new { eventId = eventId });
        }
    }
}
