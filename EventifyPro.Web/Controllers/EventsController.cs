using EventifyPro.BLL.Services.Interfaces;

namespace EventifyPro.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventService _eventService;
        private readonly ICategoryService _categoryService;
        private readonly ITicketTypeService _ticketTypeService;
        private readonly IBookingService _bookingService;
        private readonly ITicketService _ticketService;
        private readonly IPdfService _pdfService;
        private readonly IQRService _qrService;
        private readonly IReviewService _reviewService;
        private readonly IUploadHelper _uploadHelper;
        private readonly ISavedEventService _savedEventService;
        private readonly IConverter _pdfConverter;
        private readonly IImageUploadService _imageUploadService;
        private readonly IAiService _aiService;

        public EventsController(
            IEventService eventService,
            ICategoryService categoryService,
            ITicketTypeService ticketTypeService,
            IBookingService bookingService,
            ITicketService ticketService,
            IPdfService pdfService,
            IQRService qrService,
            IReviewService reviewService,
            IUploadHelper uploadHelper,
            ISavedEventService savedEventService,
            IConverter pdfConverter,
            IImageUploadService imageUploadService,
            IAiService aiService)
        {
            _eventService = eventService;
            _categoryService = categoryService;
            _ticketTypeService = ticketTypeService;
            _bookingService = bookingService;
            _ticketService = ticketService;
            _pdfService = pdfService;
            _qrService = qrService;
            _reviewService = reviewService;
            _uploadHelper = uploadHelper;
            _savedEventService = savedEventService;
            _pdfConverter = pdfConverter;
            _imageUploadService = imageUploadService;
            _aiService = aiService;
        }

        // 1. Browse Events (Search and Filters)
        [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "*" }, Tags = new[] { "events-cache-tag" })]
        public async Task<IActionResult> Index(EventSearchViewModel filter, CancellationToken cancellationToken)
        {


            var filterDto = new EventFilterDto
            {
                Title = filter.Title,
                CategoryId = filter.CategoryId,
                City = filter.City,
                StartDateFrom = filter.StartDateFrom.UserInputToUtc(),
                StartDateTo = filter.StartDateTo.UserInputToUtc(),
                Status = "Published", // Guests can only view published events
                PageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber,
                PageSize = 9, // Nice grid size
                SortBy = filter.SortBy ?? "StartDate",
                IsDescending = filter.IsDescending
            };

            var pagedResult = await _eventService.SearchAsync(filterDto, cancellationToken);
            var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);

            ViewBag.Categories = categoriesResult.IsSuccess ? categoriesResult.Data : new List<CategoryDto>();
            ViewBag.Cities = new List<string> { "Cairo", "Alexandria", "Giza", "Mansoura", "Tanta", "Hurghada", "Sharm El Sheikh" };

            var model = new EventListViewModel
            {
                Filter = filter,
                Events = pagedResult.Data.Adapt<List<EventSummaryViewModel>>(),
                PageNumber = filterDto.PageNumber,
                PageSize = filterDto.PageSize,
                TotalCount = pagedResult.TotalCount
            };

            return View(model);
        }

        // 2. Event Detail Page
        [OutputCache(Duration = 300, VaryByRouteValueNames = new[] { "id" }, Tags = new[] { "events-cache-tag" })]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var detailResult = await _eventService.GetDetailAsync(id, cancellationToken);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                return NotFound();
            }

            var ticketTypesResult = await _ticketTypeService.GetByEventAsync(id, cancellationToken);
            ViewBag.TicketTypes = ticketTypesResult.IsSuccess ? ticketTypesResult.Data : new List<TicketTypeResponseDto>();

            // Calculate review permission logic
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canUserReview = false;

            if (detailResult.Data != null)
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    var canReviewResult = await _reviewService.CanUserReviewAsync(userId, id, cancellationToken);
                    canUserReview = canReviewResult.IsSuccess && canReviewResult.Data;
                }
            }

            ViewBag.CanUserReview = canUserReview;

            var isSaved = false;
            if (!string.IsNullOrEmpty(userId))
            {
                var isSavedResult = await _savedEventService.IsEventSavedByUserAsync(userId, id, cancellationToken);
                isSaved = isSavedResult.IsSuccess && isSavedResult.Data;
            }

            var reviewsSummaryResult = await _reviewService.GetEventReviewsSummaryAsync(id, cancellationToken);
            var reviewsSummary = reviewsSummaryResult.IsSuccess && reviewsSummaryResult.Data != null
                ? reviewsSummaryResult.Data
                : new EventReviewsSummaryDto();

            var totalReviews = reviewsSummary.TotalReviews;
            var averageRating = reviewsSummary.AverageRating;
            var ratingDistribution = reviewsSummary.RatingDistribution;
            var ratingPercentages = reviewsSummary.RatingPercentages;

            ViewBag.ReviewsCount = totalReviews;
            ViewBag.AverageRating = averageRating;
            ViewBag.RatingDistribution = ratingDistribution;
            ViewBag.RatingPercentages = ratingPercentages;

            var reviewsResult = await _reviewService.GetByEventAsync(id, cancellationToken);
            ViewBag.Reviews = reviewsResult.IsSuccess && reviewsResult.Data != null ? reviewsResult.Data.Take(4).ToList() : new List<ReviewResponseDto>();

            var viewModel = detailResult.Data!.Adapt<EventDetailViewModel>();
            viewModel.IsSaved = isSaved;
            return View(viewModel);
        }

        // 3. Ticket Booking Selection (Book Now)
        [HttpGet]
        public async Task<IActionResult> Book(int id, int? ticketTypeId, int? waitingListId, CancellationToken cancellationToken)
        {
            var detailResult = await _eventService.GetDetailAsync(id, cancellationToken);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                return NotFound();
            }

            var ticketTypesResult = await _ticketTypeService.GetByEventAsync(id, cancellationToken);
            var ticketTypes = ticketTypesResult.IsSuccess && ticketTypesResult.Data != null 
                ? ticketTypesResult.Data 
                : new List<TicketTypeResponseDto>();

            if (!ticketTypes.Any())
            {
                TempData["BookingError"] = "No tickets are currently available for this event.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            ViewBag.TicketTypes = ticketTypes;
            ViewBag.SelectedTicketTypeId = ticketTypeId ?? ticketTypes.First().Id;
            ViewBag.WaitingListId = waitingListId;

            var viewModel = detailResult.Data!.Adapt<EventDetailViewModel>();
            return View(viewModel);
        }

        // 4. Ticket Booking POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("booking-limit")]
        public IActionResult Book(int eventId, List<BookingItemRequestDto> items, int? waitingListId)
        {
            var activeItems = items.Where(i => i.Quantity > 0).ToList();
            if (!activeItems.Any())
            {
                TempData["BookingError"] = "Please select at least one ticket.";
                return RedirectToAction("Details", new { id = eventId });
            }

            // Verify login status (Step 5 of User Flow)
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                // Construct returnUrl manually to pass list of items via query string
                var queryParams = new List<string> { $"eventId={eventId}" };
                for (int i = 0; i < activeItems.Count; i++)
                {
                    queryParams.Add($"items[{i}].TicketTypeId={activeItems[i].TicketTypeId}");
                    queryParams.Add($"items[{i}].Quantity={activeItems[i].Quantity}");
                }
                if (waitingListId.HasValue)
                {
                    queryParams.Add($"waitingListId={waitingListId.Value}");
                }
                var returnUrl = "/Events/BookConfirmed?" + string.Join("&", queryParams);

                TempData["InfoMessage"] = "Please sign in to complete your booking.";
                return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
            }

            // Redirect to BookConfirmed (generating the URL query string manually for complex list binding)
            var confirmParams = new List<string> { $"eventId={eventId}" };
            for (int i = 0; i < activeItems.Count; i++)
            {
                confirmParams.Add($"items[{i}].TicketTypeId={activeItems[i].TicketTypeId}");
                confirmParams.Add($"items[{i}].Quantity={activeItems[i].Quantity}");
            }
            if (waitingListId.HasValue)
            {
                confirmParams.Add($"waitingListId={waitingListId.Value}");
            }
            return Redirect("/Events/BookConfirmed?" + string.Join("&", confirmParams));
        }

        // 5. Booking Confirmation after Login
        [HttpGet]
        [Authorize]
        [EnableRateLimiting("booking-limit")]
        public async Task<IActionResult> BookConfirmed(int eventId, List<BookingItemRequestDto> items, int? waitingListId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var activeItems = items.Where(i => i.Quantity > 0).ToList();
            if (!activeItems.Any())
            {
                TempData["BookingError"] = "Please select at least one ticket.";
                return RedirectToAction("Details", new { id = eventId });
            }

            var dto = new BookingCreateDto
            {
                EventId = eventId,
                Items = activeItems,
                WaitingListId = waitingListId
            };

            var result = await _bookingService.CreatePendingAsync(dto, userId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["BookingError"] = result.Error ?? "Failed to create booking.";
                return RedirectToAction("Details", new { id = eventId });
            }

            // Redirect directly to Checkout payment step
            return RedirectToAction("Checkout", "Payment", new { id = result.Data.Id });
        }

        // 6. Attendee Tickets Page (My Tickets)
        [HttpGet]
        [Authorize]
        public IActionResult MyTickets()
        {
            return RedirectToAction("MyTickets", "Ticket");
        }

        // 7. Download PDF Ticket
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DownloadTicket(int id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

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

        // 8. Serve QR Code Image
        [HttpGet]
        [AllowAnonymous]
        public IActionResult QR(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest();
            }

            var pngBytes = _qrService.GeneratePngBytes(token);
            return File(pngBytes, "image/png");
        }

        // === Organizer Management Actions ===

        // GET: Events/OrganizerIndex
        [HttpGet]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        public async Task<IActionResult> OrganizerIndex(string? searchTerm, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var result = await _eventService.GetOrganizerEventsPagedAsync(organizerId, searchTerm, pageNumber, pageSize, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to retrieve events.";
                return RedirectToAction("Index", "Dashboard");
            }

            var viewModels = result.Data.Data.Adapt<List<EventSummaryViewModel>>();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = result.Data.TotalPages;
            ViewBag.TotalCount = result.Data.TotalCount;
            ViewBag.SearchTerm = searchTerm;

            return View(viewModels);
        }

        // GET: Events/Create
        [HttpGet]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {

            var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
            var categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                : new List<Web.ViewModels.Category.CategoryViewModel>();

            var model = new EventFormViewModel
            {
                StartDate = DateTime.UtcNow.ToEgyptTime().Date.AddDays(7).AddHours(18),
                EndDate = DateTime.UtcNow.ToEgyptTime().Date.AddDays(7).AddHours(22),
                Categories = categories
            };

            return View(model);
        }

        // POST: Events/Create
        [HttpPost]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("booking-limit")]
        public async Task<IActionResult> Create(EventFormViewModel model, IFormFile? imageFile, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            // Date validations
            if (model.StartDate.UserInputToUtc() <= DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(model.StartDate), "Event start date must be in the future.");
            }
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Event end date must be after the start date.");
            }

            // Event MaxCapacity vs Ticket Quantities validation
            var sumTicketCapacities = model.Tickets?.Sum(t => t.TotalQuantity) ?? 0;
            if (model.MaxCapacity.HasValue && sumTicketCapacities > model.MaxCapacity.Value)
            {
                ModelState.AddModelError(nameof(model.MaxCapacity), $"The total quantity of all ticket types (currently {sumTicketCapacities}) cannot exceed the event's maximum capacity of {model.MaxCapacity.Value}.");
            }

            // Tickets validation
            if (model.Tickets == null || !model.Tickets.Any())
            {
                ModelState.AddModelError(string.Empty, "At least one ticket type must be defined for the event.");
            }
            else
            {
                for (int i = 0; i < model.Tickets.Count; i++)
                {
                    var ticket = model.Tickets[i];
                    if (string.IsNullOrWhiteSpace(ticket.Name))
                    {
                        ModelState.AddModelError($"Tickets[{i}].Name", "Ticket name is required.");
                    }
                    if (ticket.Price < 0)
                    {
                        ModelState.AddModelError($"Tickets[{i}].Price", "Price cannot be negative.");
                    }
                    if (ticket.TotalQuantity <= 0)
                    {
                        ModelState.AddModelError($"Tickets[{i}].TotalQuantity", "Quantity must be at least 1.");
                    }
                    
                    // Ticket Sale dates validation
                    if (ticket.SaleStartDate.HasValue && ticket.SaleStartDate.Value.UserInputToUtc() < DateTime.UtcNow.AddMinutes(-5))
                    {
                        ModelState.AddModelError($"Tickets[{i}].SaleStartDate", "Ticket sale start date must be in the future.");
                    }
                    if (ticket.SaleStartDate.HasValue && ticket.SaleStartDate.Value > model.EndDate)
                    {
                        ModelState.AddModelError($"Tickets[{i}].SaleStartDate", "Ticket sale start date cannot be after the event ends.");
                    }
                    if (ticket.SaleEndDate.HasValue && ticket.SaleEndDate.Value > model.EndDate)
                    {
                        ModelState.AddModelError($"Tickets[{i}].SaleEndDate", "Ticket sale end date cannot be after the event ends.");
                    }
                    if (ticket.SaleStartDate.HasValue && ticket.SaleEndDate.HasValue && ticket.SaleEndDate.Value <= ticket.SaleStartDate.Value)
                    {
                        ModelState.AddModelError($"Tickets[{i}].SaleEndDate", "Ticket sale end date must be after the sale start date.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
                model.Categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                    ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                    : new List<Web.ViewModels.Category.CategoryViewModel>();
                return View(model);
            }

            string? imageUrl = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadResult = await _imageUploadService.UploadImageAsync(imageFile, "events", cancellationToken: cancellationToken);
                if (uploadResult.IsFailure)
                {
                    ModelState.AddModelError("imageFile", uploadResult.Error!);
                    var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
                    model.Categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                        ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                        : new List<Web.ViewModels.Category.CategoryViewModel>();
                    return View(model);
                }
                imageUrl = uploadResult.Data;
            }

            var dto = new EventCreateDto
            {
                Title = model.Title?.Trim() ?? string.Empty,
                Description = model.Description?.Trim() ?? string.Empty,
                StartDate = model.StartDate.UserInputToUtc(),
                EndDate = model.EndDate.UserInputToUtc(),
                Location = model.Location?.Trim() ?? string.Empty,
                City = model.City?.Trim() ?? string.Empty,
                ImageUrl = imageUrl,
                MaxCapacity = model.MaxCapacity,
                CategoryId = model.CategoryId
            };

            var result = await _eventService.CreateAsync(dto, organizerId, cancellationToken);
            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create event.");
                var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
                model.Categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                    ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                    : new List<Web.ViewModels.Category.CategoryViewModel>();
                return View(model);
            }

            int eventId = result.Data!.Id;

            // Save tickets if any were provided in the wizard
            if (model.Tickets != null)
            {
                foreach (var ticketModel in model.Tickets)
                {
                    var ticketDto = new TicketTypeCreateDto
                    {
                        EventId = eventId,
                        Name = ticketModel.Name?.Trim() ?? string.Empty,
                        Price = ticketModel.Price,
                        TotalQuantity = ticketModel.TotalQuantity,
                        Description = ticketModel.Description?.Trim(),
                        SaleStartDate = ticketModel.SaleStartDate.UserInputToUtc(),
                        SaleEndDate = ticketModel.SaleEndDate.UserInputToUtc()
                    };

                    var ticketResult = await _ticketTypeService.AddToEventAsync(eventId, ticketDto, organizerId, cancellationToken);
                    if (ticketResult.IsFailure)
                    {
                        // Rollback event creation to maintain atomicity
                        await _eventService.DeleteAsync(eventId, organizerId, cancellationToken);
                        ModelState.AddModelError(string.Empty, $"Failed to add ticket type '{ticketModel.Name}': {ticketResult.Error}");
                        
                        var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
                        model.Categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                            ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                            : new List<Web.ViewModels.Category.CategoryViewModel>();
                        return View(model);
                    }
                }
            }

            TempData["SuccessMessage"] = "Event created successfully with ticket types and is pending review.";
            return RedirectToAction(nameof(OrganizerIndex));
        }

        // GET: Events/Edit/5
        [HttpGet]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var eventDetailResult = await _eventService.GetDetailAsync(id, cancellationToken);
            if (!eventDetailResult.IsSuccess || eventDetailResult.Data == null)
            {
                return NotFound("Event not found.");
            }

            if (eventDetailResult.Data.OrganizerId != organizerId)
            {
                return Forbid();
            }

            if (eventDetailResult.Data.Status == EventStatus.Cancelled.ToString() || 
                eventDetailResult.Data.Status == EventStatus.Completed.ToString())
            {
                TempData["ErrorMessage"] = "Cannot edit cancelled or completed events.";
                return RedirectToAction(nameof(OrganizerIndex));
            }

            var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
            var categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                : new List<Web.ViewModels.Category.CategoryViewModel>();

            var model = eventDetailResult.Data.Adapt<EventFormViewModel>();
            model.StartDate = model.StartDate.ToEgyptTime();
            model.EndDate = model.EndDate.ToEgyptTime();
            model.Categories = categories;
            ViewBag.TicketsSold = eventDetailResult.Data.TotalTicketsSold;
            ViewBag.Status = eventDetailResult.Data.Status;
            ViewBag.ReviewNotes = eventDetailResult.Data.ReviewNotes;

            return View(model);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("booking-limit")]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model, IFormFile? imageFile, CancellationToken cancellationToken)
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

            var eventDetailResult = await _eventService.GetDetailAsync(id, cancellationToken);
            if (!eventDetailResult.IsSuccess || eventDetailResult.Data == null)
            {
                return NotFound("Event not found.");
            }

            if (eventDetailResult.Data.OrganizerId != organizerId)
            {
                return Forbid();
            }

            if (eventDetailResult.Data.Status == EventStatus.Cancelled.ToString() || 
                eventDetailResult.Data.Status == EventStatus.Completed.ToString())
            {
                TempData["ErrorMessage"] = "Cannot edit cancelled or completed events.";
                return RedirectToAction(nameof(OrganizerIndex));
            }

            // Date validations
            if (model.StartDate.UserInputToUtc() <= DateTime.UtcNow && model.StartDate.UserInputToUtc() != eventDetailResult.Data.StartDate)
            {
                ModelState.AddModelError(nameof(model.StartDate), "Event start date must be in the future.");
            }
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Event end date must be after the start date.");
            }
 
            // Reschedule validation for sold tickets
            var ticketsSold = eventDetailResult.Data.TotalTicketsSold;
            if (ticketsSold > 0)
            {
                if (model.StartDate.UserInputToUtc() != eventDetailResult.Data.StartDate)
                {
                    ModelState.AddModelError(nameof(model.StartDate), "Cannot change the start date because tickets have already been sold. Please cancel the event if rescheduling is necessary.");
                }
                if (model.EndDate.UserInputToUtc() != eventDetailResult.Data.EndDate)
                {
                    ModelState.AddModelError(nameof(model.EndDate), "Cannot change the end date because tickets have already been sold. Please cancel the event if rescheduling is necessary.");
                }
            }

            // Validate MaxCapacity is not less than the sum of all ticket types' total capacities
            var sumTicketCapacities = await _ticketTypeService.GetSumOfTotalQuantityByEventAsync(id, null, cancellationToken);

            if (model.MaxCapacity.HasValue && model.MaxCapacity.Value < sumTicketCapacities)
            {
                ModelState.AddModelError(nameof(model.MaxCapacity), $"The event's maximum capacity (currently {model.MaxCapacity.Value}) cannot be less than the sum of all ticket types' total quantities ({sumTicketCapacities}).");
            }



            if (!ModelState.IsValid)
            {
                var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
                model.Categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                    ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                    : new List<Web.ViewModels.Category.CategoryViewModel>();
                ViewBag.TicketsSold = eventDetailResult.Data.TotalTicketsSold;
                ViewBag.Status = eventDetailResult.Data.Status;
                ViewBag.ReviewNotes = eventDetailResult.Data.ReviewNotes;
                return View(model);
            }

            string? imageUrl = eventDetailResult.Data.ImageUrl;
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadResult = await _imageUploadService.UploadImageAsync(imageFile, "events", cancellationToken: cancellationToken);
                if (uploadResult.IsFailure)
                {
                    ModelState.AddModelError("imageFile", uploadResult.Error!);
                    var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
                    model.Categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                        ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                        : new List<Web.ViewModels.Category.CategoryViewModel>();
                    ViewBag.TicketsSold = eventDetailResult.Data.TotalTicketsSold;
                    ViewBag.Status = eventDetailResult.Data.Status;
                    ViewBag.ReviewNotes = eventDetailResult.Data.ReviewNotes;
                    return View(model);
                }

                // Delete old file if exists
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    _uploadHelper.DeleteFile(imageUrl);
                }

                imageUrl = uploadResult.Data;
            }

            var dto = new EventUpdateDto
            {
                Id = model.Id,
                Title = model.Title?.Trim() ?? string.Empty,
                Description = model.Description?.Trim() ?? string.Empty,
                StartDate = model.StartDate.UserInputToUtc(),
                EndDate = model.EndDate.UserInputToUtc(),
                Location = model.Location?.Trim() ?? string.Empty,
                City = model.City?.Trim() ?? string.Empty,
                ImageUrl = imageUrl,
                MaxCapacity = model.MaxCapacity,
                CategoryId = model.CategoryId
            };

            var result = await _eventService.UpdateAsync(id, dto, organizerId, cancellationToken);
            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update event.");
                var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
                model.Categories = categoriesResult.IsSuccess && categoriesResult.Data != null
                    ? categoriesResult.Data.Adapt<List<Web.ViewModels.Category.CategoryViewModel>>()
                    : new List<Web.ViewModels.Category.CategoryViewModel>();
                ViewBag.TicketsSold = eventDetailResult.Data.TotalTicketsSold;
                return View(model);
            }

            TempData["SuccessMessage"] = "Event updated successfully and is pending review.";
            return RedirectToAction(nameof(OrganizerIndex));
        }

        // POST: Events/Cancel/5
        [HttpPost]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Cancel(int id, string reason, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "A cancellation reason is required.";
                return RedirectToAction(nameof(OrganizerIndex));
            }

            var result = await _eventService.CancelAsync(id, organizerId, reason, cancellationToken);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to cancel event.";
            }
            else
            {
                TempData["SuccessMessage"] = "Event cancelled successfully, bookings refunded and attendees notified.";
            }

            return RedirectToAction(nameof(OrganizerIndex));
        }

        // POST: Events/Delete/5
        [HttpPost]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            // Retrieve event details to get the image path before soft-deleting
            var eventResult = await _eventService.GetDetailAsync(id, cancellationToken);

            if (!eventResult.IsSuccess || eventResult.Data == null || eventResult.Data.OrganizerId != organizerId)
            {
                return NotFound("Event not found.");
            }

            var eventEntity = eventResult.Data;

            var result = await _eventService.DeleteAsync(id, organizerId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to delete event.";
            }
            else
            {
                // Delete physical image file to free space on disk
                if (!string.IsNullOrEmpty(eventEntity.ImageUrl))
                {
                    _uploadHelper.DeleteFile(eventEntity.ImageUrl);
                }
                TempData["SuccessMessage"] = "Event deleted successfully.";
            }

            return RedirectToAction(nameof(OrganizerIndex));
        }

        // === Admin Review Actions ===

        // GET: Events/PendingReview
        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> PendingReview(int page = 1, CancellationToken cancellationToken = default)
        {
            var result = await _eventService.GetPendingReviewAsync(page, 10, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                return View(new List<EventSummaryViewModel>());
            }

            var data = result.Data;
            var viewModels = data.Data.Adapt<List<EventSummaryViewModel>>();

            ViewBag.CurrentPage = data.PageNumber;
            ViewBag.TotalPages = data.TotalPages;

            return View(viewModels);
        }

        // POST: Events/Approve/5
        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Challenge();
            }

            var result = await _eventService.ApproveAsync(id, adminId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to approve event.";
                TempData["AdminError"] = result.Error ?? "Failed to approve event.";
            }
            else
            {
                TempData["SuccessMessage"] = "Event approved and published successfully.";
                TempData["AdminSuccess"] = "Event approved and published successfully.";
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(PendingReview));
        }

        // POST: Events/Reject/5
        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Reject(int id, string reason, CancellationToken cancellationToken)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "A rejection reason is required.";
                TempData["AdminError"] = "A rejection reason is required.";
                var refererUrl = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(refererUrl))
                {
                    return Redirect(refererUrl);
                }
                return RedirectToAction(nameof(PendingReview));
            }

            var result = await _eventService.RejectAsync(id, adminId, reason, cancellationToken);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to reject event.";
                TempData["AdminError"] = result.Error ?? "Failed to reject event.";
            }
            else
            {
                TempData["SuccessMessage"] = "Event rejected and organizer notified.";
                TempData["AdminSuccess"] = "Event rejected and organizer notified.";
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(PendingReview));
        }

        // GET: Events/CheckDuplicateTitle
        [HttpGet]
        public async Task<IActionResult> CheckDuplicateTitle(string title, int? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return Json(true);
            }

            var titleTrimmed = title.Trim();
            var exists = await _eventService.ExistsByTitleAsync(titleTrimmed, id, cancellationToken);

            return Json(!exists);
        }

        // POST: Events/Restore/5
        [HttpPost]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var result = await _eventService.RestoreAsync(id, organizerId, cancellationToken);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to restore event.";
            }
            else
            {
                TempData["SuccessMessage"] = "Event restored successfully as a draft and is pending review.";
            }

            return RedirectToAction(nameof(OrganizerIndex));
        }

        // GET: Events/Performance/5
        [HttpGet]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        public async Task<IActionResult> Performance(int id, CancellationToken cancellationToken)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Challenge();
            }

            var result = await _eventService.GetPerformanceAsync(id, organizerId, cancellationToken);
            if (result.IsFailure || result.Data == null)
            {
                return NotFound(result.Error ?? "Event performance data not found.");
            }

            var viewModel = result.Data.Adapt<EventPerformanceViewModel>();
            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Organizer)]
        [TypeFilter(typeof(VerifiedOrganizerFilter))]
        public async Task<IActionResult> ExportPerformance(int id, string format, CancellationToken cancellationToken)
        {
            try
            {
                var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(organizerId))
                {
                    return Challenge();
                }

                var result = await _eventService.GetPerformanceAsync(id, organizerId, cancellationToken);
                if (result.IsFailure || result.Data == null)
                {
                    return NotFound(result.Error ?? "Event performance data not found.");
                }

                var data = result.Data;

                if (format.ToLower() == "csv")
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Metric Name,Value");
                    csv.AppendLine($"Event Title,{data.Title.Replace(",", " ")}");
                    csv.AppendLine($"Total Revenue (EGP),{data.TotalRevenue:0.00}");
                    csv.AppendLine($"Tickets Sold,{data.TotalTicketsSold}");
                    csv.AppendLine($"Total Capacity,{data.TotalCapacity}");
                    csv.AppendLine($"Sold Out Percentage,{data.SoldPercentage:F2}%");
                    csv.AppendLine($"Waiting List Size,{data.WaitingListCount}");
                    csv.AppendLine($"Confirmed Bookings,{data.ConfirmedBookings}");
                    csv.AppendLine($"Pending Bookings,{data.PendingBookings}");
                    csv.AppendLine($"Cancelled Bookings,{data.CancelledBookings}");

                    csv.AppendLine();
                    csv.AppendLine("Ticket Tier,Price (EGP),Total Capacity,Sold Quantity,Sold Percentage");
                    foreach (var tier in data.TicketTypes)
                    {
                        csv.AppendLine($"{tier.Name.Replace(",", " ")},{tier.Price:0.00},{tier.TotalQuantity},{tier.SoldQuantity},{tier.SoldPercentage:F2}%");
                    }

                    return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Performance_{data.Title.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.csv");
                }
                else if (format.ToLower() == "excel")
                {
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Performance Report");
                        worksheet.Cell("A1").Value = $"Performance Report: {data.Title}";
                        worksheet.Cell("A1").Style.Font.Bold = true;
                        worksheet.Cell("A1").Style.Font.FontSize = 14;
                        worksheet.Range("A1:E1").Merge();

                        worksheet.Cell("A2").Value = $"Generated At: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                        worksheet.Cell("A2").Style.Font.Italic = true;

                        // Section 1: KPI Overview
                        worksheet.Cell("A4").Value = "Key Metric";
                        worksheet.Cell("B4").Value = "Value";
                        worksheet.Range("A4:B4").Style.Font.Bold = true;
                        worksheet.Range("A4:B4").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#ede9fe");

                        worksheet.Cell("A5").Value = "Total Revenue";
                        worksheet.Cell("B5").Value = data.TotalRevenue;
                        worksheet.Cell("B5").Style.NumberFormat.Format = "#,##0.00 EGP";

                        worksheet.Cell("A6").Value = "Tickets Sold";
                        worksheet.Cell("B6").Value = $"{data.TotalTicketsSold} / {data.TotalCapacity}";

                        worksheet.Cell("A7").Value = "Sold Out Rate";
                        worksheet.Cell("B7").Value = $"{data.SoldPercentage:F1}%";

                        worksheet.Cell("A8").Value = "Waiting List Count";
                        worksheet.Cell("B8").Value = data.WaitingListCount;

                        worksheet.Cell("A9").Value = "Confirmed Bookings";
                        worksheet.Cell("B9").Value = data.ConfirmedBookings;

                        worksheet.Cell("A10").Value = "Pending Bookings";
                        worksheet.Cell("B10").Value = data.PendingBookings;

                        worksheet.Cell("A11").Value = "Cancelled Bookings";
                        worksheet.Cell("B11").Value = data.CancelledBookings;

                        // Section 2: Ticket Tiers
                        worksheet.Cell("D4").Value = "Ticket Tier";
                        worksheet.Cell("E4").Value = "Price";
                        worksheet.Cell("F4").Value = "Sold";
                        worksheet.Cell("G4").Value = "Capacity";
                        worksheet.Cell("H4").Value = "Sold Out %";
                        worksheet.Range("D4:H4").Style.Font.Bold = true;
                        worksheet.Range("D4:H4").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#ede9fe");

                        int row = 5;
                        foreach (var tier in data.TicketTypes)
                        {
                            worksheet.Cell(row, 4).Value = tier.Name;
                            worksheet.Cell(row, 5).Value = tier.Price;
                            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 EGP";
                            worksheet.Cell(row, 6).Value = tier.SoldQuantity;
                            worksheet.Cell(row, 7).Value = tier.TotalQuantity;
                            worksheet.Cell(row, 8).Value = $"{tier.SoldPercentage:F1}%";
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();

                        using (var stream = new System.IO.MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();
                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Performance_{data.Title.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
                        }
                    }
                }
                else if (format.ToLower() == "pdf")
                {
                    var html = new System.Text.StringBuilder();
                    html.Append($@"
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; }}
        h2 {{ color: #5b21b6; border-bottom: 2px solid #5b21b6; padding-bottom: 8px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th {{ background-color: #ede9fe; color: #5b21b6; text-align: left; padding: 10px; font-size: 12px; text-transform: uppercase; border: 1px solid #ddd; }}
        td {{ padding: 10px; border: 1px solid #ddd; font-size: 12px; }}
        tr:nth-child(even) {{ background-color: #fcfbfe; }}
        .meta {{ font-size: 11px; color: #666; margin-bottom: 20px; }}
        .section-title {{ font-size: 14px; font-weight: bold; margin-top: 30px; color: #5b21b6; }}
    </style>
</head>
<body>
    <h2>Performance Report: {data.Title}</h2>
    <div class='meta'>Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>

    <div class='section-title'>Key Performance Indicators</div>
    <table>
        <thead>
            <tr>
                <th>Metric Name</th>
                <th>Value</th>
            </tr>
        </thead>
        <tbody>
            <tr><td>Total Revenue</td><td><strong>{data.TotalRevenue:N2} EGP</strong></td></tr>
            <tr><td>Tickets Sold</td><td>{data.TotalTicketsSold} / {data.TotalCapacity}</td></tr>
            <tr><td>Sold Out Rate</td><td>{data.SoldPercentage:F1}%</td></tr>
            <tr><td>Waiting List Count</td><td>{data.WaitingListCount}</td></tr>
            <tr><td>Confirmed Bookings</td><td>{data.ConfirmedBookings}</td></tr>
            <tr><td>Pending Bookings</td><td>{data.PendingBookings}</td></tr>
            <tr><td>Cancelled Bookings</td><td>{data.CancelledBookings}</td></tr>
        </tbody>
    </table>

    <div class='section-title'>Ticket Tier Details</div>
    <table>
        <thead>
            <tr>
                <th>Tier Name</th>
                <th>Price</th>
                <th>Sold Quantity</th>
                <th>Total Capacity</th>
                <th>Sold Out Rate</th>
            </tr>
        </thead>
        <tbody>");

                    foreach (var tier in data.TicketTypes)
                    {
                        html.Append($@"
            <tr>
                <td>{System.Net.WebUtility.HtmlEncode(tier.Name)}</td>
                <td>{tier.Price:N2} EGP</td>
                <td>{tier.SoldQuantity}</td>
                <td>{tier.TotalQuantity}</td>
                <td>{tier.SoldPercentage:F1}%</td>
            </tr>");
                    }

                    html.Append(@"
        </tbody>
    </table>
</body>
</html>");

                    var doc = new HtmlToPdfDocument
                    {
                        GlobalSettings =
                        {
                            ColorMode = ColorMode.Color,
                            Orientation = Orientation.Portrait,
                            PaperSize = PaperKind.A4,
                            Margins = new MarginSettings { Top = 15, Bottom = 15, Left = 15, Right = 15 }
                        },
                        Objects =
                        {
                            new ObjectSettings
                            {
                                PagesCount = true,
                                HtmlContent = html.ToString(),
                                WebSettings = { DefaultEncoding = "utf-8" }
                            }
                        }
                    };

                    var pdfBytes = await Task.Run(() => _pdfConverter.Convert(doc));
                    return File(pdfBytes, "application/pdf", $"Performance_{data.Title.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pdf");
                }

                return BadRequest("Invalid export format.");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred during report export.";
                return RedirectToAction(nameof(Performance), new { id });
            }
        }

        [HttpPost("Events/GenerateDescription")]
        [Authorize(Roles = RoleNames.Organizer)]
        public async Task<IActionResult> GenerateDescription([FromBody] GenerateDescriptionRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                return Json(new { success = false, error = "Event Title is required to generate description." });
            }

            try
            {
                var description = await _aiService.GenerateEventDescriptionAsync(
                    request.Title,
                    request.City ?? "",
                    request.Category ?? "",
                    cancellationToken);

                return Json(new { success = true, description });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"An error occurred while generating the description: {ex.Message}" });
            }
        }

    }

    public class GenerateDescriptionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
