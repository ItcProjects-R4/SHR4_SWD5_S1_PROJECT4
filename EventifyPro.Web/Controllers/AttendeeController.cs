


namespace EventifyPro.Web.Controllers;

[Authorize(Roles = RoleNames.Attendee)]
public class AttendeeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDashboardService _dashboardService;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailService _emailService;
    private readonly ILogger<AttendeeController> _logger;
    private readonly ISavedEventService _savedEventService;
    private readonly INotificationService _notificationService;
    private readonly IEventService _eventService;
    private readonly IMemoryCache _cache;

    public AttendeeController(
        UserManager<ApplicationUser> userManager,
        IDashboardService dashboardService,
        IWebHostEnvironment environment,
        IEmailService emailService,
        ILogger<AttendeeController> _logger,
        ISavedEventService savedEventService,
        INotificationService notificationService,
        IEventService eventService,
        IMemoryCache cache)
    {
        _userManager = userManager;
        _dashboardService = dashboardService;
        _environment = environment;
        _emailService = emailService;
        this._logger = _logger;
        _savedEventService = savedEventService;
        _notificationService = notificationService;
        _eventService = eventService;
        _cache = cache;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["ErrorMessage"] = "Please verify your email before accessing your attendee dashboard.";
                context.Result = RedirectToAction("ConfirmEmail", "Account", new { email = user.Email });
                return;
            }
        }

        await next();
    }

    /// <summary>
    /// GET: Renders the Attendee Dashboard index view with personal statistics.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var result = await _dashboardService.GetAttendeeDashboardAsync(userId, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to load dashboard data.";
            return RedirectToAction("Index", "Home");
        }

        var data = result.Data;

        var viewModel = new AttendeeDashboardViewModel
        {
            FullName = data.FullName,
            Email = data.Email,
            ProfileImageUrl = data.ProfileImageUrl,
            TotalBookings = data.TotalBookings,
            ConfirmedBookings = data.ConfirmedBookings,
            PendingBookings = data.PendingBookings,
            TotalTickets = data.TotalTickets,
            ActiveTickets = data.ActiveTickets,
            UsedTickets = data.UsedTickets,
            ExpiredTickets = data.ExpiredTickets,
            TotalReviews = data.TotalReviews,
            HasPhoneNumber = data.HasPhoneNumber,
            HasProfileImage = data.HasProfileImage,
            IsEmailConfirmed = data.IsEmailConfirmed,
            ProfileCompletionPercentage = data.ProfileCompletionPercentage,
            UpcomingEvent = data.UpcomingEvent != null ? new AttendeeUpcomingEventViewModel
            {
                EventId = data.UpcomingEvent.EventId,
                TicketId = data.UpcomingEvent.TicketId,
                Title = data.UpcomingEvent.Title,
                Description = data.UpcomingEvent.Description,
                Location = data.UpcomingEvent.Location,
                City = data.UpcomingEvent.City,
                ImageUrl = data.UpcomingEvent.ImageUrl,
                StartDate = data.UpcomingEvent.StartDate,
                DaysRemaining = data.UpcomingEvent.DaysRemaining,
                HoursRemaining = data.UpcomingEvent.HoursRemaining,
                TicketCount = data.UpcomingEvent.TicketCount,
                BookingReference = data.UpcomingEvent.BookingReference
            } : null,
            ReviewPrompt = data.ReviewPrompt != null ? new AttendeeReviewPromptViewModel
            {
                EventId = data.ReviewPrompt.EventId,
                EventTitle = data.ReviewPrompt.EventTitle,
                EventDate = data.ReviewPrompt.EventDate
            } : null,
            RecentActivity = data.RecentActivity.Select(a => new AttendeeActivityViewModel
            {
                Title = a.Title,
                Description = a.Description,
                Icon = a.Icon,
                Tone = a.Tone,
                Date = a.Date
            }).ToList(),
            RecentBookings = data.RecentBookings.Select(b => new BookingSummaryViewModel
            {
                Id = b.Id,
                UserId = b.UserId,
                EventId = b.EventId,
                EventTitle = b.EventTitle,
                TotalAmount = b.TotalAmount,
                Status = b.Status,
                BookingReference = b.BookingReference,
                BookingDate = b.BookingDate
            }).ToList(),
            RecommendedEvents = data.RecommendedEvents.Select(e => new EventSummaryViewModel
            {
                Id = e.Id,
                Title = e.Title,
                City = e.City,
                ImageUrl = e.ImageUrl,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                CategoryName = e.CategoryName,
                MinPrice = e.MinPrice,
                OrganizerName = e.OrganizerName
            }).ToList()
        };

        return View(viewModel);
    }



    /// <summary>
    /// GET: Renders the Attendee Profile edit view.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var viewModel = new AttendeeProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfileImageUrl = user.ProfileImageUrl
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST: Handles the update profile details and profile picture secure uploads.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(AttendeeProfileViewModel model, IFormFile? profilePicture)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Validate unique email check
        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already taken by another user.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.ProfileImageUrl = user.ProfileImageUrl;
            return View("Profile", model);
        }

        // Handle Profile Picture securely
        if (profilePicture != null && profilePicture.Length > 0)
        {
            var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(string.Empty, "Only image files (.jpg, .jpeg, .png) are allowed.");
                model.ProfileImageUrl = user.ProfileImageUrl;
                return View("Profile", model);
            }

            var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!allowedContentTypes.Contains(profilePicture.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError(string.Empty, "Invalid image type format.");
                model.ProfileImageUrl = user.ProfileImageUrl;
                return View("Profile", model);
            }

            if (profilePicture.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(string.Empty, "Profile picture size cannot exceed 2 MB.");
                model.ProfileImageUrl = user.ProfileImageUrl;
                return View("Profile", model);
            }

            // Generate secure unique path using GUID
            var fileName = Guid.NewGuid().ToString() + extension;
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(fileStream);
            }

            // Remove old picture file if present
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfileImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete old profile image file: {Path}", oldFilePath);
                    }
                }
            }

            user.ProfileImageUrl = "/uploads/profiles/" + fileName;
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.NormalizedEmail = model.Email.ToUpperInvariant();
        user.PhoneNumber = model.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.ProfileImageUrl = user.ProfileImageUrl;
            return View("Profile", model);
        }

        TempData["SuccessMessage"] = "Profile updated successfully!";
        _cache.Remove($"AttendeeDashboard_{userId}");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: Handles password change when the user knows their current password.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var profileModel = new AttendeeProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl
            };
            return View("Profile", profileModel);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            var profileModel = new AttendeeProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl
            };
            TempData["ChangePasswordError"] = string.Join("<br/>", result.Errors.Select(e => e.Description));
            return View("Profile", profileModel);
        }

        TempData["SuccessMessage"] = "Password changed successfully!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: Generates and sends a 6-digit verification OTP to the user's email for password resetting.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPasswordOtp(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "User session expired." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            return Json(new { success = false, message = "User email not found." });
        }

        // Generate 6-digit random code
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        // Save OTP to AspNetUserTokens
        await _userManager.SetAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtp", otpCode);
        var expiryString = DateTime.UtcNow.AddMinutes(15).ToString("O");
        await _userManager.SetAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtpExpiry", expiryString);

        try
        {
            // Send OTP email
            await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, otpCode, cancellationToken);
            return Json(new { success = true, message = "OTP verification code sent to " + user.Email });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP to {Email}", user.Email);
            return Json(new { success = false, message = "Failed to send email. Please try again later." });
        }
    }

    /// <summary>
    /// POST: Verifies the 6-digit OTP and resets the password for the logged-in attendee.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordWithOtp(ResetPasswordWithOtpViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var profileModel = new AttendeeProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl
            };
            ViewData["ShowOtpForm"] = true;
            return View("Profile", profileModel);
        }

        // Retrieve tokens
        var savedOtp = await _userManager.GetAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtp");
        var expiryString = await _userManager.GetAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtpExpiry");

        if (string.IsNullOrEmpty(savedOtp) || string.IsNullOrEmpty(expiryString) || savedOtp != model.OtpCode.Trim())
        {
            ModelState.AddModelError(nameof(model.OtpCode), "Invalid OTP code.");
            return await RerenderProfileWithOtpError(user, "Invalid OTP code.");
        }

        if (!DateTime.TryParse(expiryString, out var expiry) || expiry < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(model.OtpCode), "OTP code has expired. Please request a new one.");
            return await RerenderProfileWithOtpError(user, "OTP code has expired. Please request a new one.");
        }

        // Generate password reset token and reset password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return await RerenderProfileWithOtpError(user, string.Join("<br/>", result.Errors.Select(e => e.Description)));
        }

        // Clean tokens on success
        await _userManager.RemoveAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtp");
        await _userManager.RemoveAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtpExpiry");

        TempData["SuccessMessage"] = "Password has been reset successfully!";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RerenderProfileWithOtpError(ApplicationUser user, string errorMessage)
    {
        var profileModel = new AttendeeProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfileImageUrl = user.ProfileImageUrl
        };
        ViewData["ShowOtpForm"] = true;
        TempData["OtpError"] = errorMessage;
        return View("Profile", profileModel);
    }

    /// <summary>
    /// GET: Renders the attendee's saved events.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SavedEvents(int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        const int pageSize = 6;
        var result = await _savedEventService.GetSavedEventsForUserAsync(userId, page, pageSize, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to retrieve saved events.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    /// <summary>
    /// GET: Renders the attendee's booked (registered) events.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MyEvents(string status = "all", int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        const int pageSize = 6;
        var result = await _eventService.GetAttendeeEventsPagedAsync(userId, status, page, pageSize, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to retrieve registered events.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.CurrentStatus = status;
        return View(result.Data);
    }

    /// <summary>
    /// POST: Toggles saving/unsaving an event.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSave(int eventId, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var result = await _savedEventService.ToggleSaveEventAsync(userId, eventId, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to toggle save event.";
        }
        else
        {
            TempData["SuccessMessage"] = result.Data 
                ? "Event saved successfully!" 
                : "Event removed from saved list.";
        }

        // Redirect back to referring page or Event Details
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer))
        {
            return Redirect(referer);
        }

        return RedirectToAction("Details", "Events", new { id = eventId });
    }
}
