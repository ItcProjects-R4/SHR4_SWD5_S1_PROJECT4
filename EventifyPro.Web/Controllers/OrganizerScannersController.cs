using System.Security.Claims;
using Eventify.Domain.Constants;
using EventifyPro.BLL.DTOs.Auth;
using EventifyPro.BLL.Services.Interfaces;
using EventifyPro.Web.ViewModels.OrganizerScanners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventifyPro.Controllers;

[Authorize(Roles = RoleNames.Organizer)]
public class OrganizerScannersController : Controller
{
    private readonly IAuthService _authService;

    public OrganizerScannersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateScannerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScannerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _authService.CreateScannerForOrganizerAsync(new CreateScannerDto
        {
            FullName = model.FullName,
            Email = model.Email,
            Password = model.Password
        }, organizerId);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Scanner account created successfully.";
        return RedirectToAction(nameof(Create));
    }
}
