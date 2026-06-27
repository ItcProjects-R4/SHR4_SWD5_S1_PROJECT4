
namespace EventifyPro.Web.Controllers
{
    [Route("Admin")]
    public class AdminFeedbackController : AdminBaseController
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminFeedbackController> _logger;

        public AdminFeedbackController(
            UserManager<ApplicationUser> userManager,
            IAdminService adminService,
            ILogger<AdminFeedbackController> logger) : base(userManager)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpPost("ApproveFeedback")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> ApproveFeedback(int id, CancellationToken cancellationToken)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                {
                    return Challenge();
                }

                var result = await _adminService.ApproveFeedbackAsync(id, adminId, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error approving feedback.";
                    return RedirectToAction("Index", "Admin");
                }

                TempData["AdminFeedbackSuccess"] = "Feedback approved and will now appear on the landing page.";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving feedback");
                TempData["AdminError"] = "Error approving feedback.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("DeleteFeedback")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> DeleteFeedback(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _adminService.DeleteFeedbackAsync(id, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error deleting feedback.";
                    return RedirectToAction("Index", "Admin");
                }

                TempData["AdminFeedbackSuccess"] = "Feedback deleted successfully.";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feedback");
                TempData["AdminError"] = "Error deleting feedback.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("ApproveOrganizer")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> ApproveOrganizer(string userId, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required.");
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                {
                    return Challenge();
                }

                var result = await _adminService.ApproveOrganizerAsync(userId, adminId, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Failed to approve organizer.";
                    return RedirectToAction("Index", "Admin");
                }

                TempData["AdminSuccess"] = "Organizer profile approved and activated successfully.";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving organizer");
                TempData["AdminError"] = "Error approving organizer.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("RejectOrganizer")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> RejectOrganizer(string userId, string reason, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required.");
                }

                var result = await _adminService.RejectOrganizerAsync(userId, reason, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Failed to reject organizer.";
                    return RedirectToAction("Index", "Admin");
                }

                TempData["AdminSuccess"] = "Organizer profile has been rejected and user account downgraded.";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting organizer");
                TempData["AdminError"] = "Error rejecting organizer.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("BulkApproveOrganizers")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> BulkApproveOrganizers(List<string> userIds, CancellationToken cancellationToken)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                {
                    return Challenge();
                }

                if (userIds == null || !userIds.Any())
                {
                    TempData["AdminError"] = "No organizers selected.";
                    return RedirectToAction("Index", "Admin");
                }

                var result = await _adminService.BulkApproveOrganizersAsync(userIds, adminId, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Failed to approve selected organizers.";
                }
                else
                {
                    TempData["AdminSuccess"] = "Selected organizers have been approved and activated.";
                }

                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk approving organizers");
                TempData["AdminError"] = "Error bulk approving organizers.";
                return RedirectToAction("Index", "Admin");
            }
        }
    }
}
