
namespace EventifyPro.Web.Controllers
{
    [Route("Admin")]
    public class AdminUsersController : AdminBaseController
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            UserManager<ApplicationUser> userManager,
            IAdminService adminService,
            ILogger<AdminUsersController> logger) : base(userManager)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpGet("Users")]
        public async Task<IActionResult> Users(string searchTerm, string roleFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var result = await _adminService.GetUsersPageAsync(searchTerm, roleFilter, page, cancellationToken);
                if (result.IsFailure || result.Data == null)
                {
                    TempData["AdminError"] = result.Error ?? "Error loading users list.";
                    return View("~/Views/Admin/Users.cshtml", new List<ApplicationUser>());
                }

                var data = result.Data;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.RoleFilter = roleFilter;
                ViewBag.PageNumber = page ?? 1;
                ViewBag.TotalPages = data.TotalPages;
                ViewBag.TotalCount = data.TotalCount;
                ViewBag.UserRoles = data.UserRoles;

                ViewBag.TotalUsersCount = data.TotalUsersCount;
                ViewBag.ActiveUsersCount = data.ActiveUsersCount;
                ViewBag.InactiveUsersCount = data.InactiveUsersCount;
                ViewBag.OrganizersCount = data.OrganizersCount;
                ViewBag.AdminsCount = data.AdminsCount;
                ViewBag.ScannersCount = data.ScannersCount;
                ViewBag.AttendeesCount = data.AttendeesCount;

                return View("~/Views/Admin/Users.cshtml", data.Users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                TempData["AdminError"] = "Error loading users list.";
                return View("~/Views/Admin/Users.cshtml", new List<ApplicationUser>());
            }
        }

        [HttpPost("ManageUserRole")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> ManageUserRole(string userId, string newRole, bool isActive, string searchTerm, string roleFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == currentAdminId)
                {
                    TempData["AdminError"] = "You cannot modify your own role or active status.";
                    return RedirectToAction(nameof(Users), new { searchTerm, roleFilter, page });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return NotFound("User not found.");
                }

                // Check if user is currently an Admin
                var isCurrentAdmin = await _userManager.IsInRoleAsync(user, RoleNames.Admin);
                if (isCurrentAdmin)
                {
                    var isDeactivating = !isActive;
                    var isChangingRoleToNonAdmin = !string.IsNullOrWhiteSpace(newRole) && newRole != RoleNames.Admin;

                    if (isDeactivating || isChangingRoleToNonAdmin)
                    {
                        var activeAdmins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                        var activeAdminsCount = activeAdmins.Count(u => u.IsActive);
                        if (activeAdminsCount <= 1)
                        {
                            TempData["AdminError"] = "Cannot demote or deactivate the last remaining active Administrator in the system.";
                            return RedirectToAction(nameof(Users), new { searchTerm, roleFilter, page });
                        }
                    }
                }

                var result = await _adminService.ManageUserRoleAsync(userId, newRole, isActive, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Failed to update user status.";
                    return RedirectToAction(nameof(Users), new { searchTerm, roleFilter, page });
                }

                // Force user security stamp update to reject active sessions if role/status changed
                await _userManager.UpdateSecurityStampAsync(user);

                TempData["AdminSuccess"] = $"User '{user.FullName}' has been updated successfully.";
                return RedirectToAction(nameof(Users), new { searchTerm, roleFilter, page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing user role");
                TempData["AdminError"] = "Error managing user role.";
                return RedirectToAction(nameof(Users), new { searchTerm, roleFilter, page });
            }
        }

        [HttpPost("UnlockUser")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> UnlockUser(string userId, string searchTerm, string roleFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return NotFound();
                }

                var result = await _adminService.UnlockUserAsync(userId, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Failed to unlock user account.";
                    return RedirectToAction(nameof(Users), new { searchTerm, roleFilter, page });
                }

                TempData["AdminSuccess"] = $"User account for '{user.FullName}' has been unlocked successfully.";
                return RedirectToAction(nameof(Users), new { searchTerm, roleFilter, page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user");
                TempData["AdminError"] = "Error unlocking user.";
                return RedirectToAction(nameof(Users), new { searchTerm, roleFilter, page });
            }
        }

        [HttpGet("Users/Details/{id}")]
        public async Task<IActionResult> UserDetails(string id, string returnUrl, CancellationToken cancellationToken)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var result = await _adminService.GetUserDetailsAsync(id, cancellationToken);
                if (result.IsFailure || result.Data == null)
                {
                    TempData["AdminError"] = result.Error ?? "Error loading user details.";
                    return RedirectToAction(nameof(Users));
                }

                var dto = result.Data;
                var viewModel = new AdminUserDetailsViewModel
                {
                    User = dto.User,
                    PrimaryRole = dto.PrimaryRole,
                    Bookings = dto.Bookings,
                    Payments = dto.Payments,
                    Reviews = dto.Reviews,
                    AuditLogs = dto.AuditLogs
                };

                ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? Url.Action(nameof(Users)) : returnUrl;

                return View("~/Views/Admin/UserDetails.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for User ID: {UserId}", id);
                TempData["AdminError"] = "Error loading user details.";
                return RedirectToAction(nameof(Users));
            }
        }
    }
}
