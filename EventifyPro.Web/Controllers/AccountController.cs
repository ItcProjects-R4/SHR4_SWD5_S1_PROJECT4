


namespace EventifyPro.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IImageUploadService _imageUploadService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;

    public AccountController(
        IAuthService authService, 
        IImageUploadService imageUploadService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _authService = authService;
        _imageUploadService = imageUploadService;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }


    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login-limit")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var dto = new LoginDto
        {
            Email = model.Email,
            Password = model.Password,
            RememberMe = model.RememberMe
        };

        var result = await _authService.LoginAsync(dto);

        if (result.IsFailure)
        {
            if (result.Error != null && result.Error.Contains("not verified"))
            {
                TempData["SuccessMessage"] = result.Error;
                return RedirectToAction(nameof(ConfirmEmail), new { email = model.Email });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData.Remove("ErrorMessage");

        var roleResult = await _authService.GetUserPrimaryRoleByEmailAsync(model.Email);
        var normalizedRole = NormalizeRole(roleResult.IsSuccess ? roleResult.Data : string.Empty);

        if (normalizedRole == RoleNames.Organizer)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var isVerified = await _authService.IsOrganizerVerifiedAsync(user.Id);
                if (!isVerified)
                {
                    return RedirectToAction("PendingApproval", "Organizer");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectByRole(normalizedRole);
    }


    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (string.Equals(model.Role, RoleNames.Organizer, StringComparison.OrdinalIgnoreCase) && 
            string.IsNullOrWhiteSpace(model.OrganizationName))
        {
            ModelState.AddModelError(nameof(model.OrganizationName), "Organization Name is required for organizers.");
        }

        string? logoUrl = null;
        if (string.Equals(model.Role, RoleNames.Organizer, StringComparison.OrdinalIgnoreCase) && 
            model.LogoFile != null && model.LogoFile.Length > 0)
        {
            var uploadResult = await _imageUploadService.UploadImageAsync(model.LogoFile, "organizers");
            if (uploadResult.IsFailure)
            {
                ModelState.AddModelError(nameof(model.LogoFile), uploadResult.Error!);
            }
            else
            {
                logoUrl = uploadResult.Data;
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        var dto = new RegisterDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.UserName,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            Role = model.Role,
            OrganizationName = model.OrganizationName,
            BusinessPhone = model.BusinessPhone,
            WebsiteUrl = model.WebsiteUrl,
            CommercialRegister = model.CommercialRegister,
            TaxNumber = model.TaxNumber,
            LogoUrl = logoUrl
        };

        var result = await _authService.RegisterPublicUserAsync(dto);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Account created successfully. Please verify your email using the verification code (OTP) sent to you.";
        return RedirectToAction(nameof(ConfirmEmail), new { email = model.Email });
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp-limit")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.ForgotPasswordAsync(model.Email);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        return RedirectToAction(nameof(VerifyPasswordResetOtp), new { email = model.Email });
    }

    [HttpGet]
    public IActionResult VerifyPasswordResetOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new VerifyPasswordResetOtpViewModel { Email = email };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp-limit")]
    public async Task<IActionResult> VerifyPasswordResetOtp(VerifyPasswordResetOtpViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.VerifyPasswordResetOtpAsync(model.Email, model.OtpCode);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        return RedirectToAction(nameof(ResetPassword), new { email = model.Email, token = result.Data });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp-limit")]
    public async Task<IActionResult> ResendPasswordResetOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Login));

        var result = await _authService.ForgotPasswordAsync(email);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(VerifyPasswordResetOtp), new { email = email });
        }

        TempData["SuccessMessage"] = "A new verification code (OTP) has been sent to your email.";
        return RedirectToAction(nameof(VerifyPasswordResetOtp), new { email = email });
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Login));
        }

        // Prevent leaking password reset token in Referrer headers
        Response.Headers["Referrer-Policy"] = "no-referrer";

        var model = new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var dto = new ResetPasswordDto
        {
            Email = model.Email,
            Token = model.Token,
            NewPassword = model.NewPassword,
            ConfirmPassword = model.ConfirmPassword
        };

        var result = await _authService.ResetPasswordAsync(dto);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Your password has been reset successfully. You can log in now.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ConfirmEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new ConfirmEmailViewModel { Email = email };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp-limit")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.ConfirmEmailAsync(model.Email, model.OtpCode);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Your email has been verified successfully. You can log in now.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> CheckUserName(string userName)
    {
        var available = await _authService.IsUserNameAvailableAsync(userName);

        return Json(new
        {
            available,
            message = available ? "Username is available." : "Username is already taken."
        });
    }

    [HttpGet]
    public IActionResult CheckEmail(string email, string type)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Login));
        }

        ViewBag.ActionType = type;
        return View("CheckEmail", email);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp-limit")]
    public async Task<IActionResult> ResendVerificationEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Login));

        var result = await _authService.ResendVerificationEmailAsync(email);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
        }
        else
        {
            TempData["SuccessMessage"] = "A new verification code (OTP) has been sent to your email.";
        }

        return RedirectToAction(nameof(ConfirmEmail), new { email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action("GoogleLoginCallback", "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return Challenge(properties, "Google");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "Error loading external login information.";
            return RedirectToAction(nameof(Login));
        }

        if (info.LoginProvider == "Google")
        {
            var emailVerified = info.Principal.FindFirst("email_verified")?.Value;
            if (string.Equals(emailVerified, "false", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Your Google account email is not verified. Please verify it with Google first.";
                return RedirectToAction(nameof(Login));
            }

            var googleEmail = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(googleEmail))
            {
                var localUser = await _userManager.FindByEmailAsync(googleEmail);
                if (localUser != null && !localUser.EmailConfirmed)
                {
                    localUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(localUser);

                    // Enqueue the Welcome email since this confirms their email and completes registration
                    await _outboxService.EnqueueAsync(
                        "Email.Welcome",
                        new EventifyPro.BLL.Services.Implementations.OutboxService.WelcomePayload
                        {
                            RecipientEmail = localUser.Email!,
                            RecipientName = localUser.FullName
                        },
                        DateTime.UtcNow.AddSeconds(5));
                }
            }
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            // Update last login
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                if (!user.IsActive)
                {
                    await _signInManager.SignOutAsync();
                    TempData["ErrorMessage"] = ErrorMessages.User.AccountDisabled;
                    return RedirectToAction(nameof(Login));
                }

                TempData.Remove("ErrorMessage");

                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = NormalizeRole(roles.FirstOrDefault());
                
                if (primaryRole == RoleNames.Organizer)
                {
                    var isVerified = await _authService.IsOrganizerVerifiedAsync(user.Id);
                    if (!isVerified)
                    {
                        return RedirectToAction("PendingApproval", "Organizer");
                    }
                }

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectByRole(primaryRole);
            }
        }

        // Get email claim from Google
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Email claim not received from Google.";
            return RedirectToAction(nameof(Login));
        }

        // Check if user already exists in the database
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            if (!existingUser.IsActive)
            {
                TempData["ErrorMessage"] = ErrorMessages.User.AccountDisabled;
                return RedirectToAction(nameof(Login));
            }

            // Link external login to existing user
            var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
            if (addLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                TempData.Remove("ErrorMessage");
                existingUser.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(existingUser);

                var roles = await _userManager.GetRolesAsync(existingUser);
                var primaryRole = NormalizeRole(roles.FirstOrDefault());

                if (primaryRole == RoleNames.Organizer)
                {
                    var isVerified = await _authService.IsOrganizerVerifiedAsync(existingUser.Id);
                    if (!isVerified)
                    {
                        return RedirectToAction("PendingApproval", "Organizer");
                    }
                }

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectByRole(primaryRole);
            }
        }

        // If user does not exist, redirect to ExternalLoginConfirmation to complete details
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

        var model = new ExternalLoginConfirmationViewModel
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        ViewData["ReturnUrl"] = returnUrl;
        return View("ExternalLoginConfirmation", model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginConfirmation(string? returnUrl = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "Error loading external login information.";
            return RedirectToAction(nameof(Login));
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

        var model = new ExternalLoginConfirmationViewModel
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        // Retrieve external login info from external cookie
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["ErrorMessage"] = "Error loading external login information.";
            return RedirectToAction(nameof(Login));
        }

        if (string.Equals(model.Role, RoleNames.Organizer, StringComparison.OrdinalIgnoreCase) && 
            string.IsNullOrWhiteSpace(model.OrganizationName))
        {
            ModelState.AddModelError(nameof(model.OrganizationName), "Organization Name is required for organizers.");
        }

        string? logoUrl = null;
        if (string.Equals(model.Role, RoleNames.Organizer, StringComparison.OrdinalIgnoreCase) && 
            model.LogoFile != null && model.LogoFile.Length > 0)
        {
            var uploadResult = await _imageUploadService.UploadImageAsync(model.LogoFile, "organizers");
            if (uploadResult.IsFailure)
            {
                ModelState.AddModelError(nameof(model.LogoFile), uploadResult.Error!);
            }
            else
            {
                logoUrl = uploadResult.Data;
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Generate unique username
        var emailPrefix = model.Email.Split('@')[0];
        var cleanedUserName = new string(emailPrefix
            .Select(c => (char.IsLetterOrDigit(c) || c == '_') ? c : '_')
            .ToArray());

        var userName = cleanedUserName;
        int suffix = 1;
        while (await _userManager.FindByNameAsync(userName) != null)
        {
            userName = $"{cleanedUserName}_{suffix++}";
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = model.Email.Trim(),
            FullName = $"{model.FirstName.Trim()} {model.LastName.Trim()}",
            PhoneNumber = string.Equals(model.Role, RoleNames.Organizer, StringComparison.OrdinalIgnoreCase) ? model.BusinessPhone?.Trim() : null,
            IsActive = true,
            EmailConfirmed = true, // Google already verified their email
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            ModelState.AddModelError(string.Empty, errors);
            return View(model);
        }

        var roleName = NormalizeRole(model.Role);
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "Failed to assign role.");
            return View(model);
        }

        if (roleName == RoleNames.Organizer)
        {
            var profile = new OrganizerProfile
            {
                UserId = user.Id,
                OrganizationName = string.IsNullOrWhiteSpace(model.OrganizationName)
                    ? user.FullName
                    : model.OrganizationName.Trim(),
                BusinessPhone = model.BusinessPhone?.Trim(),
                WebsiteUrl = model.WebsiteUrl?.Trim(),
                CommercialRegister = model.CommercialRegister?.Trim(),
                TaxNumber = model.TaxNumber?.Trim(),
                LogoUrl = logoUrl,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.OrganizerProfiles.AddAsync(profile);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Failed to create organizer profile. Please try again.");
                return View(model);
            }
        }

        // Link external login provider to the newly created user
        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "Failed to link external Google login.");
            return View(model);
        }

        // Sign in
        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData.Remove("ErrorMessage");
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Enqueue the Welcome email since this is their first external login/registration
        await _outboxService.EnqueueAsync(
            "Email.Welcome",
            new EventifyPro.BLL.Services.Implementations.OutboxService.WelcomePayload
            {
                RecipientEmail = user.Email!,
                RecipientName = user.FullName
            },
            DateTime.UtcNow.AddSeconds(5));

        if (roleName == RoleNames.Organizer)
        {
            // Redirect to Organizer PendingApproval
            return RedirectToAction("PendingApproval", "Organizer");
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectByRole(roleName);
    }


    /// <summary>
    /// Normalise role string so the redirect switch always matches the
    /// <see cref="RoleNames"/> constants regardless of submitted casing.
    /// </summary>
    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return string.Empty;

        string[] knownRoles =
        [
            RoleNames.Admin,
            RoleNames.Organizer,
            RoleNames.Attendee,
            RoleNames.Scanner
        ];

        return knownRoles.FirstOrDefault(
                   r => string.Equals(r, role.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? string.Empty;
    }

    private IActionResult RedirectByRole(string normalizedRole) =>
        normalizedRole switch
        {
            RoleNames.Admin => RedirectToAction("Admin", "Dashboard"),
            RoleNames.Organizer => RedirectToAction("Organizer", "Dashboard"),
            RoleNames.Scanner => RedirectToAction("Index", "Scanner"),
            RoleNames.Attendee => RedirectToAction("Index", "Attendee"),
            _ => RedirectToAction("Index", "Home")  // fallback
        };
}
