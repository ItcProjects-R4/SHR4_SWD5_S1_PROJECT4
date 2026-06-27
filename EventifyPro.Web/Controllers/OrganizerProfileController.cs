
namespace EventifyPro.Web.Controllers;

[Authorize(Roles = RoleNames.Organizer)]
[TypeFilter(typeof(VerifiedOrganizerFilter))]
public class OrganizerProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUploadHelper _uploadHelper;
    private readonly IEmailService _emailService;
    private readonly ILogger<OrganizerProfileController> _logger;
    private readonly IImageUploadService _imageUploadService;

    public OrganizerProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUploadHelper uploadHelper,
        IEmailService emailService,
        ILogger<OrganizerProfileController> logger,
        IImageUploadService imageUploadService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _uploadHelper = uploadHelper;
        _emailService = emailService;
        _logger = logger;
        _imageUploadService = imageUploadService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.Users
            .Include(u => u.OrganizerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null)
        {
            return NotFound();
        }

        var model = new OrganizerProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfileImageUrl = user.ProfileImageUrl,
            OrganizationName = user.OrganizerProfile?.OrganizationName,
            Bio = user.OrganizerProfile?.Bio,
            WebsiteUrl = user.OrganizerProfile?.WebsiteUrl,
            FacebookUrl = user.OrganizerProfile?.FacebookUrl,
            LinkedInUrl = user.OrganizerProfile?.LinkedInUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(OrganizerProfileViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.Users
            .Include(u => u.OrganizerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null)
        {
            return NotFound();
        }

        // 1. Repopulate Email to prevent read-only validation failure
        model.Email = user.Email ?? string.Empty;
        ModelState.Remove(nameof(model.Email));



        if (!ModelState.IsValid)
        {
            model.ProfileImageUrl = user.ProfileImageUrl;
            return View(model);
        }

        try
        {
            // 3. Process password change FIRST (BEFORE saving other profile changes)
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is required to change password.");
                    model.ProfileImageUrl = user.ProfileImageUrl;
                    return View(model);
                }

                var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.ProfileImageUrl = user.ProfileImageUrl;
                    return View(model);
                }
            }

            // 4. Save other profile changes since password validation has succeeded
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var uploadResult = await _imageUploadService.UploadImageAsync(model.ProfilePicture, "profiles");
                if (uploadResult.IsFailure)
                {
                    ModelState.AddModelError(nameof(model.ProfilePicture), uploadResult.Error!);
                    model.ProfileImageUrl = user.ProfileImageUrl;
                    return View(model);
                }

                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                {
                    _uploadHelper.DeleteFile(user.ProfileImageUrl);
                }

                user.ProfileImageUrl = uploadResult.Data;
            }

            // Update basic user info
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            // Update organizer business details
            if (user.OrganizerProfile == null)
            {
                user.OrganizerProfile = new OrganizerProfile
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
            }
            user.OrganizerProfile.OrganizationName = model.OrganizationName ?? string.Empty;
            user.OrganizerProfile.Bio = model.Bio;
            user.OrganizerProfile.WebsiteUrl = model.WebsiteUrl;
            user.OrganizerProfile.FacebookUrl = model.FacebookUrl;
            user.OrganizerProfile.LinkedInUrl = model.LinkedInUrl;
            user.OrganizerProfile.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.ProfileImageUrl = user.ProfileImageUrl;
                return View(model);
            }

            // Refresh user session after changes
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            TempData["ErrorMessage"] = "Error updating profile details.";
            model.ProfileImageUrl = user.ProfileImageUrl;
            return View(model);
        }
    }

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
            _logger.LogError(ex, "Failed to send password reset OTP to organizer {Email}", user.Email);
            return Json(new { success = false, message = "Failed to send email. Please try again later." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordWithOtp(ResetPasswordWithOtpViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var user = await _userManager.Users
            .Include(u => u.OrganizerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var profileModel = new OrganizerProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                OrganizationName = user.OrganizerProfile?.OrganizationName,
                Bio = user.OrganizerProfile?.Bio,
                WebsiteUrl = user.OrganizerProfile?.WebsiteUrl,
                FacebookUrl = user.OrganizerProfile?.FacebookUrl,
                LinkedInUrl = user.OrganizerProfile?.LinkedInUrl
            };
            ViewData["ShowOtpForm"] = true;
            return View("Index", profileModel);
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

        TempData["SuccessMessage"] = "Password has been reset successfully!";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RerenderProfileWithOtpError(ApplicationUser user, string errorMessage)
    {
        var profileModel = new OrganizerProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfileImageUrl = user.ProfileImageUrl,
            OrganizationName = user.OrganizerProfile?.OrganizationName,
            Bio = user.OrganizerProfile?.Bio,
            WebsiteUrl = user.OrganizerProfile?.WebsiteUrl,
            FacebookUrl = user.OrganizerProfile?.FacebookUrl,
            LinkedInUrl = user.OrganizerProfile?.LinkedInUrl
        };
        ViewData["ShowOtpForm"] = true;
        TempData["OtpError"] = errorMessage;
        return View("Index", profileModel);
    }
}
