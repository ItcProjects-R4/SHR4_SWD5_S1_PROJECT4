
namespace EventifyPro.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly IUserService _userService;
        private readonly IBookingService _bookingService;
        private readonly ITicketService _ticketService;
        private readonly IEventService _eventService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IHomeService homeService, 
            IUserService userService,
            IBookingService bookingService,
            ITicketService ticketService,
            IEventService eventService,
            IMemoryCache cache,
            ILogger<HomeController> logger)
        {
            _homeService = homeService;
            _userService = userService;
            _bookingService = bookingService;
            _ticketService = ticketService;
            _eventService = eventService;
            _cache = cache;
            _logger = logger;
        }

        [OutputCache(Duration = 300, Tags = new[] { "events-cache-tag" })]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            try
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole(Eventify.Domain.Constants.RoleNames.Attendee))
                    {
                        return RedirectToAction("Index", "Attendee");
                    }
                    if (User.IsInRole(Eventify.Domain.Constants.RoleNames.Admin))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    if (User.IsInRole(Eventify.Domain.Constants.RoleNames.Organizer))
                    {
                        return RedirectToAction("Index", "Organizer");
                    }
                    if (User.IsInRole(Eventify.Domain.Constants.RoleNames.Scanner))
                    {
                        return RedirectToAction("Index", "Scanner");
                    }
                }

                const string LandingCacheKey = "GuestLandingPageData";
                if (!_cache.TryGetValue(LandingCacheKey, out (List<LandingFeedbackViewModel> Feedback, List<CategoryDto> Categories, List<EventSummaryViewModel> FeaturedEvents, int TotalTickets, int TotalOrganizers, int TotalEvents, EventSummaryViewModel? HeroEvent) cacheData))
                {
                    _logger.LogDebug("Guest landing page cache miss. Querying BLL HomeService.");
                    var landingResult = await _homeService.GetLandingPageDataAsync(cancellationToken);
                    if (landingResult.IsFailure || landingResult.Data == null)
                    {
                        return View(new HomeIndexViewModel());
                    }

                    var data = landingResult.Data;

                    var feedback = data.ApprovedFeedback.Select(f => new LandingFeedbackViewModel
                    {
                        DisplayName = string.IsNullOrWhiteSpace(f.DisplayName) ? "Eventify Pro User" : f.DisplayName,
                        Message = f.Message,
                        CreatedAt = f.CreatedAt
                    }).ToList();

                    var featuredEvents = data.FeaturedEvents.Adapt<List<EventSummaryViewModel>>();

                    // Select hero event
                    var heroEvent = featuredEvents.FirstOrDefault();
                    if (heroEvent == null)
                    {
                        var latestEventResult = await _eventService.SearchAsync(new EventFilterDto
                        {
                            Status = "Published",
                            PageNumber = 1,
                            PageSize = 1,
                            SortBy = "StartDate",
                            IsDescending = false
                        }, cancellationToken);
                        if (latestEventResult.Data != null && latestEventResult.Data.Any())
                        {
                            heroEvent = latestEventResult.Data.First().Adapt<EventSummaryViewModel>();
                        }
                    }

                    cacheData = (feedback, data.Categories, featuredEvents, data.TotalTickets, data.TotalOrganizers, data.TotalEvents, heroEvent);

                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                    _cache.Set(LandingCacheKey, cacheData, cacheOptions);
                }

                var model = new HomeIndexViewModel
                {
                    ApprovedFeedback = cacheData.Feedback,
                    Categories = cacheData.Categories,
                    FeaturedEvents = cacheData.FeaturedEvents,
                    TotalTicketsSold = cacheData.TotalTickets,
                    TotalActiveOrganizers = cacheData.TotalOrganizers,
                    TotalSuccessfulEvents = cacheData.TotalEvents,
                    HeroEvent = cacheData.HeroEvent,
                    IsAuthenticated = false
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rendering the guest landing page Index");
                ViewBag.DatabaseError = true;
                ViewBag.ErrorMessage = "We are currently experiencing technical difficulties. Some features may not be available.";

                var fallbackModel = new HomeIndexViewModel
                {
                    ApprovedFeedback = new List<LandingFeedbackViewModel>(),
                    Categories = new List<CategoryDto>(),
                    FeaturedEvents = new List<EventSummaryViewModel>(),
                    TotalTicketsSold = 0,
                    TotalActiveOrganizers = 0,
                    TotalSuccessfulEvents = 0,
                    HeroEvent = null,
                    IsAuthenticated = false
                };
                return View(fallbackModel);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult FAQs()
        {
            return View();
        }

        public IActionResult HelpCenter()
        {
            return View();
        }

        public IActionResult Feedback()
        {
            return View(new FeedbackViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(FeedbackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Feedback", model);
            }

            try
            {
                var dto = new FeedbackCreateDto
                {
                    Name = model.Name?.Trim() ?? string.Empty,
                    Email = model.Email?.Trim() ?? string.Empty,
                    Message = model.Message.Trim()
                };

                var result = await _homeService.SubmitFeedbackAsync(dto);
                if (result.IsFailure)
                {
                    ModelState.AddModelError(string.Empty, result.Error ?? "Failed to submit feedback.");
                    return View("Feedback", model);
                }

                TempData["FeedbackSuccess"] = "Thanks for your feedback. It will appear on the homepage after admin approval.";
                return RedirectToAction(nameof(Feedback));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while submitting feedback");
                ModelState.AddModelError(string.Empty, "We are currently experiencing technical difficulties. Please try again later.");
                return View("Feedback", model);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature?.Error != null)
            {
                var exception = exceptionHandlerPathFeature.Error;
                var path = exceptionHandlerPathFeature.Path;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                SystemErrorLogger.LogError(exception, path, userId);
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
