using Eventify.Domain.Constants;
using EventifyPro.BLL.DTOs.Auth;
using EventifyPro.BLL.Services.Interfaces;
using EventifyPro.Web.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;

namespace EventifyPro.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }


    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
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
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        // Get user's role using IsInRole checks
        string userRole = string.Empty;
        if (User.IsInRole(RoleNames.Admin))
            userRole = RoleNames.Admin;
        else if (User.IsInRole(RoleNames.Organizer))
            userRole = RoleNames.Organizer;
        else if (User.IsInRole(RoleNames.Scanner))
            userRole = RoleNames.Scanner;
        else if (User.IsInRole(RoleNames.Attendee))
            userRole = RoleNames.Attendee;

        return RedirectByRole(userRole);
    }


    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
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
            Role = model.Role
        };

        var result = await _authService.RegisterPublicUserAsync(dto);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Account created successfully. You can sign in now.";
        return RedirectToAction(nameof(Login));
    }

    public IActionResult ForgotPassword()
    {
        return View();
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

    public IActionResult CheckEmail()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Index", "Home");
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
            RoleNames.Admin => RedirectToAction("Index", "Admin"),
            RoleNames.Organizer => RedirectToAction("Index", "Organizer"),
            RoleNames.Scanner => RedirectToAction("Index", "Scanner"),
            _ => RedirectToAction("Index", "Home")  // Attendee + fallback
        };
}
