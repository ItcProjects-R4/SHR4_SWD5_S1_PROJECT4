


using EventifyPro.Web.ViewModels.Attendee;

namespace EventifyPro.Web.Controllers
{
    [Route("Admin")]
    public class AdminController : AdminBaseController
    {
        private readonly IAdminService _adminService;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _environment;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IUploadHelper _uploadHelper;
        private readonly ILogger<AdminController> _logger;
        private readonly IImageUploadService _imageUploadService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            IAdminService adminService,
            INotificationService notificationService,
            IWebHostEnvironment environment,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IUploadHelper uploadHelper,
            ILogger<AdminController> logger,
            IImageUploadService imageUploadService) : base(userManager)
        {
            _adminService = adminService;
            _notificationService = notificationService;
            _environment = environment;
            _signInManager = signInManager;
            _emailService = emailService;
            _uploadHelper = uploadHelper;
            _logger = logger;
            _imageUploadService = imageUploadService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var result = await _adminService.GetDashboardDataAsync(cancellationToken);
                if (result.IsFailure || result.Data == null)
                {
                    TempData["AdminError"] = result.Error ?? "Failed to load dashboard data.";
                    return View("~/Views/Admin/Index.cshtml", new AdminDashboardViewModel());
                }

                var dto = result.Data;

                var model = new AdminDashboardViewModel
                {
                    PendingFeedback = dto.PendingFeedback.Select(f => new AdminFeedbackItemViewModel
                    {
                        Id = f.Id,
                        DisplayName = string.IsNullOrWhiteSpace(f.Name) ? "Eventify Pro User" : f.Name,
                        Email = f.Email,
                        Message = f.Message,
                        IsApproved = f.IsApproved,
                        CreatedAt = f.CreatedAt
                    }).ToList(),

                    ApprovedFeedback = dto.ApprovedFeedback.Select(f => new AdminFeedbackItemViewModel
                    {
                        Id = f.Id,
                        DisplayName = string.IsNullOrWhiteSpace(f.Name) ? "Eventify Pro User" : f.Name,
                        Email = f.Email,
                        Message = f.Message,
                        IsApproved = f.IsApproved,
                        CreatedAt = f.CreatedAt
                    }).ToList(),

                    PendingOrganizers = dto.PendingOrganizers.Select(p => new AdminOrganizerItemViewModel
                    {
                        UserId = p.UserId,
                        FullName = p.User.FullName,
                        Email = p.User.Email ?? string.Empty,
                        OrganizationName = p.OrganizationName,
                        BusinessPhone = p.BusinessPhone,
                        WebsiteUrl = p.WebsiteUrl,
                        CommercialRegister = p.CommercialRegister,
                        TaxNumber = p.TaxNumber,
                        LogoUrl = p.LogoUrl,
                        IsVerified = p.IsVerified,
                        CreatedAt = p.CreatedAt
                    }).ToList(),

                    VerifiedOrganizers = dto.VerifiedOrganizers.Select(p => new AdminOrganizerItemViewModel
                    {
                        UserId = p.UserId,
                        FullName = p.User.FullName,
                        Email = p.User.Email ?? string.Empty,
                        OrganizationName = p.OrganizationName,
                        BusinessPhone = p.BusinessPhone,
                        WebsiteUrl = p.WebsiteUrl,
                        CommercialRegister = p.CommercialRegister,
                        TaxNumber = p.TaxNumber,
                        LogoUrl = p.LogoUrl,
                        IsVerified = p.IsVerified,
                        CreatedAt = p.CreatedAt
                    }).ToList(),

                    PendingEvents = dto.PendingEvents.ToList()
                };

                return View("~/Views/Admin/Index.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard Index");
                TempData["AdminError"] = "Error loading dashboard data.";
                return View("~/Views/Admin/Index.cshtml", new AdminDashboardViewModel());
            }
        }

        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var model = new AdminProfileViewModel
                {
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImageUrl = user.ProfileImageUrl
                };

                return View("~/Views/Admin/Profile.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile");
                TempData["AdminError"] = "Error loading profile.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("Profile")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> Profile(AdminProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!ModelState.IsValid)
            {
                model.ProfileImageUrl = user.ProfileImageUrl;
                await PopulateAdminViewDataAsync();
                return View("~/Views/Admin/Profile.cshtml", model);
            }

            try
            {
                // 1. Process profile picture upload
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    var uploadResult = await _imageUploadService.UploadImageAsync(model.ProfilePicture, "profiles");
                    if (uploadResult.IsFailure)
                    {
                        ModelState.AddModelError("ProfilePicture", uploadResult.Error!);
                        model.ProfileImageUrl = user.ProfileImageUrl;
                        await PopulateAdminViewDataAsync();
                        return View("~/Views/Admin/Profile.cshtml", model);
                    }

                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                    {
                        _uploadHelper.DeleteFile(user.ProfileImageUrl);
                    }

                    user.ProfileImageUrl = uploadResult.Data;
                }

                // 2. Process profile info updates
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                
                // Check if email has changed
                if (user.Email != model.Email)
                {
                    var emailExists = await _userManager.FindByEmailAsync(model.Email);
                    if (emailExists != null && emailExists.Id != user.Id)
                    {
                        ModelState.AddModelError("Email", "Email address is already in use by another user.");
                        model.ProfileImageUrl = user.ProfileImageUrl;
                        await PopulateAdminViewDataAsync();
                        return View("~/Views/Admin/Profile.cshtml", model);
                    }

                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        foreach (var error in setEmailResult.Errors)
                        {
                            ModelState.AddModelError("Email", error.Description);
                        }
                        model.ProfileImageUrl = user.ProfileImageUrl;
                        await PopulateAdminViewDataAsync();
                        return View("~/Views/Admin/Profile.cshtml", model);
                    }

                    var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                    if (!setUserNameResult.Succeeded)
                    {
                        foreach (var error in setUserNameResult.Errors)
                        {
                            ModelState.AddModelError("Email", error.Description);
                        }
                        model.ProfileImageUrl = user.ProfileImageUrl;
                        await PopulateAdminViewDataAsync();
                        return View("~/Views/Admin/Profile.cshtml", model);
                    }

                    user.EmailConfirmed = true; // For Admin profile immediate change, keep it confirmed
                }

                user.UpdatedAt = DateTime.UtcNow;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.ProfileImageUrl = user.ProfileImageUrl;
                    await PopulateAdminViewDataAsync();
                    return View("~/Views/Admin/Profile.cshtml", model);
                }

                // 3. Process password change if requested
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        ModelState.AddModelError("CurrentPassword", "Current password is required to change password.");
                        model.ProfileImageUrl = user.ProfileImageUrl;
                        await PopulateAdminViewDataAsync();
                        return View("~/Views/Admin/Profile.cshtml", model);
                    }

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                    if (!changePasswordResult.Succeeded)
                    {
                        foreach (var error in changePasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        model.ProfileImageUrl = user.ProfileImageUrl;
                        await PopulateAdminViewDataAsync();
                        return View("~/Views/Admin/Profile.cshtml", model);
                    }
                }

                // Refresh user session after changes
                await _signInManager.RefreshSignInAsync(user);

                TempData["AdminSuccess"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                TempData["AdminError"] = "Error updating profile details.";
                model.ProfileImageUrl = user.ProfileImageUrl;
                await PopulateAdminViewDataAsync();
                return View("~/Views/Admin/Profile.cshtml", model);
            }
        }

        [HttpPost("SendPasswordOtp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPasswordOtp(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                return Json(new { success = false, message = "User not found or email is empty." });
            }

            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            await _userManager.SetAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtp", otpCode);
            var expiryString = DateTime.UtcNow.AddMinutes(15).ToString("O");
            await _userManager.SetAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtpExpiry", expiryString);

            try
            {
                await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, otpCode, cancellationToken);
                return Json(new { success = true, message = "OTP verification code sent to " + user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset OTP to Admin {Email}", user.Email);
                return Json(new { success = false, message = "Failed to send email. Please try again later." });
            }
        }

        [HttpPost("ResetPasswordWithOtp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordWithOtp(ResetPasswordWithOtpViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!ModelState.IsValid)
            {
                var profileModel = new AdminProfileViewModel
                {
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImageUrl = user.ProfileImageUrl
                };
                ViewData["ShowOtpForm"] = true;
                await PopulateAdminViewDataAsync();
                return View("~/Views/Admin/Profile.cshtml", profileModel);
            }

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

            await _userManager.RemoveAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtp");
            await _userManager.RemoveAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetOtpExpiry");

            TempData["AdminSuccess"] = "Password has been reset successfully!";
            return RedirectToAction(nameof(Profile));
        }

        private async Task<IActionResult> RerenderProfileWithOtpError(ApplicationUser user, string errorMessage)
        {
            var profileModel = new AdminProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl
            };
            ViewData["ShowOtpForm"] = true;
            TempData["OtpError"] = errorMessage;
            await PopulateAdminViewDataAsync();
            return View("~/Views/Admin/Profile.cshtml", profileModel);
        }

        [HttpGet("Notifications")]
        public async Task<IActionResult> Notifications()
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var sentNotificationsResult = await _adminService.GetSentNotificationsAsync(HttpContext.RequestAborted);
                var sentNotifications = (sentNotificationsResult.IsSuccess && sentNotificationsResult.Data != null) ? sentNotificationsResult.Data : new List<AdminSentNotificationDto>();

                var model = new AdminNotificationViewModel
                {
                    SentNotifications = sentNotifications.Select(n => new AdminSentNotificationViewModel
                    {
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        CreatedAt = n.CreatedAt,
                        RecipientCount = n.RecipientCount,
                        Recipient = n.Recipient
                    }).ToList()
                };

                return View("~/Views/Admin/Notifications.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications");
                TempData["AdminError"] = "Error loading notifications page.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("SendNotification")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> SendNotification(AdminNotificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAdminViewDataAsync();
                
                var sentNotificationsResult = await _adminService.GetSentNotificationsAsync(HttpContext.RequestAborted);
                var sentNotifications = (sentNotificationsResult.IsSuccess && sentNotificationsResult.Data != null) ? sentNotificationsResult.Data : new List<AdminSentNotificationDto>();

                model.SentNotifications = sentNotifications.Select(n => new AdminSentNotificationViewModel
                {
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    RecipientCount = n.RecipientCount,
                    Recipient = n.Recipient
                }).ToList();

                return View("~/Views/Admin/Notifications.cshtml", model);
            }

            try
            {
                Result<bool> result;

                if (model.IsSystemWide)
                {
                    result = await _notificationService.SendSystemNotificationAsync(
                        model.Title,
                        model.Message,
                        model.Type,
                        model.RedirectUrl,
                        HttpContext.RequestAborted);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(model.RecipientEmail))
                    {
                        ModelState.AddModelError("RecipientEmail", "Recipient email is required for targeted notifications.");
                        await PopulateAdminViewDataAsync();
                        
                        var sentNotificationsResult = await _adminService.GetSentNotificationsAsync(HttpContext.RequestAborted);
                        var sentNotifications = (sentNotificationsResult.IsSuccess && sentNotificationsResult.Data != null) ? sentNotificationsResult.Data : new List<AdminSentNotificationDto>();

                        model.SentNotifications = sentNotifications.Select(n => new AdminSentNotificationViewModel
                        {
                            Title = n.Title,
                            Message = n.Message,
                            Type = n.Type,
                            CreatedAt = n.CreatedAt,
                            RecipientCount = n.RecipientCount,
                            Recipient = n.Recipient
                        }).ToList();

                        return View("~/Views/Admin/Notifications.cshtml", model);
                    }

                    result = await _notificationService.SendTargetedNotificationAsync(
                        model.RecipientEmail,
                        model.Title,
                        model.Message,
                        model.Type,
                        model.RedirectUrl,
                        HttpContext.RequestAborted);
                }

                if (result.IsFailure)
                {
                    ModelState.AddModelError(string.Empty, result.Error ?? "Failed to send notification.");
                    await PopulateAdminViewDataAsync();
                    
                    var sentNotificationsResult = await _adminService.GetSentNotificationsAsync(HttpContext.RequestAborted);
                    var sentNotifications = (sentNotificationsResult.IsSuccess && sentNotificationsResult.Data != null) ? sentNotificationsResult.Data : new List<AdminSentNotificationDto>();

                    model.SentNotifications = sentNotifications.Select(n => new AdminSentNotificationViewModel
                    {
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        CreatedAt = n.CreatedAt,
                        RecipientCount = n.RecipientCount,
                        Recipient = n.Recipient
                    }).ToList();

                    return View("~/Views/Admin/Notifications.cshtml", model);
                }

                TempData["AdminSuccess"] = "Notification sent successfully.";
                return RedirectToAction(nameof(Notifications));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                ModelState.AddModelError(string.Empty, "An error occurred while sending the notification.");
                await PopulateAdminViewDataAsync();
                
                var sentNotificationsResult = await _adminService.GetSentNotificationsAsync(HttpContext.RequestAborted);
                var sentNotifications = (sentNotificationsResult.IsSuccess && sentNotificationsResult.Data != null) ? sentNotificationsResult.Data : new List<AdminSentNotificationDto>();

                model.SentNotifications = sentNotifications.Select(n => new AdminSentNotificationViewModel
                {
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedAt = n.CreatedAt,
                    RecipientCount = n.RecipientCount,
                    Recipient = n.Recipient
                }).ToList();

                return View("~/Views/Admin/Notifications.cshtml", model);
            }
        }

        [HttpGet("Payouts")]
        public async Task<IActionResult> Payouts()
        {
            await PopulateAdminViewDataAsync();
            var result = await _adminService.GetPayoutRequestsAsync(HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                TempData["AdminError"] = result.Error;
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/Payouts.cshtml", result.Data);
        }

        [HttpPost("Payouts/UpdateStatus")]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> UpdatePayoutStatus(int requestId, string status, string? referenceNumber, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _adminService.UpdatePayoutRequestStatusAsync(requestId, status, referenceNumber, notes, user.Id, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                TempData["AdminError"] = result.Error;
            }
            else
            {
                TempData["AdminSuccess"] = "Payout request updated successfully.";
            }

            return RedirectToAction(nameof(Payouts));
        }

    }
}
