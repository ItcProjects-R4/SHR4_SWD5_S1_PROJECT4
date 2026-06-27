namespace EventifyPro.Web.Controllers
{
    [Route("Admin")]
    public class AdminReviewsController : AdminBaseController
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminReviewsController> _logger;

        public AdminReviewsController(
            UserManager<ApplicationUser> userManager,
            IAdminService adminService,
            ILogger<AdminReviewsController> logger) : base(userManager)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpGet("Reviews")]
        public async Task<IActionResult> Reviews(string searchTerm, int? ratingFilter, bool? hiddenFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var result = await _adminService.GetReviewsPageAsync(searchTerm, ratingFilter, hiddenFilter, page, cancellationToken);
                if (result.IsFailure || result.Data == null)
                {
                    TempData["AdminError"] = result.Error ?? "Error loading reviews.";
                    return View("~/Views/Admin/Reviews.cshtml", new List<Review>());
                }

                var data = result.Data;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.RatingFilter = ratingFilter;
                ViewBag.HiddenFilter = hiddenFilter;
                ViewBag.PageNumber = page ?? 1;
                ViewBag.TotalPages = data.TotalPages;
                ViewBag.TotalCount = data.TotalCount;

                return View("~/Views/Admin/Reviews.cshtml", data.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews");
                TempData["AdminError"] = "Error loading reviews.";
                return View("~/Views/Admin/Reviews.cshtml", new List<Review>());
            }
        }

        [HttpPost("ApproveReview")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> ApproveReview(int reviewId, string searchTerm, int? ratingFilter, bool? hiddenFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                {
                    return Challenge();
                }

                var result = await _adminService.ApproveReviewAsync(reviewId, adminId, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error approving review.";
                    return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
                }

                TempData["AdminSuccess"] = "Review approved and will now be visible.";
                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving review");
                TempData["AdminError"] = "Error approving review.";
                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
        }

        [HttpPost("FlagReview")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> FlagReview(int reviewId, string? reason, string searchTerm, int? ratingFilter, bool? hiddenFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                {
                    return Challenge();
                }

                var result = await _adminService.FlagReviewAsync(reviewId, adminId, reason, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error hiding review.";
                    return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
                }

                TempData["AdminSuccess"] = "Review has been hidden.";
                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hiding review");
                TempData["AdminError"] = "Error hiding review.";
                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
        }

        [HttpPost("DeleteReview")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> DeleteReview(int reviewId, string searchTerm, int? ratingFilter, bool? hiddenFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _adminService.DeleteReviewAsync(reviewId, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error deleting review.";
                    return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
                }

                TempData["AdminSuccess"] = "Review deleted successfully.";
                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review");
                TempData["AdminError"] = "Error deleting review.";
                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
        }

        [HttpPost("BulkApproveReviews")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> BulkApproveReviews(List<int> reviewIds, string searchTerm, int? ratingFilter, bool? hiddenFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                {
                    return Challenge();
                }

                if (reviewIds == null || !reviewIds.Any())
                {
                    TempData["AdminError"] = "No reviews selected.";
                    return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
                }

                var result = await _adminService.BulkApproveReviewsAsync(reviewIds, adminId, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error approving selected reviews.";
                }
                else
                {
                    TempData["AdminSuccess"] = "Selected reviews have been approved.";
                }

                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk approving reviews");
                TempData["AdminError"] = "Error bulk approving reviews.";
                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
        }

        [HttpPost("BulkDeleteReviews")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> BulkDeleteReviews(List<int> reviewIds, string searchTerm, int? ratingFilter, bool? hiddenFilter, int? page, CancellationToken cancellationToken)
        {
            try
            {
                if (reviewIds == null || !reviewIds.Any())
                {
                    TempData["AdminError"] = "No reviews selected.";
                    return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
                }

                var result = await _adminService.BulkDeleteReviewsAsync(reviewIds, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error deleting selected reviews.";
                }
                else
                {
                    TempData["AdminSuccess"] = "Selected reviews have been deleted.";
                }

                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting reviews");
                TempData["AdminError"] = "Error bulk deleting reviews.";
                return RedirectToAction(nameof(Reviews), new { searchTerm, ratingFilter, hiddenFilter, page });
            }
        }
    }
}
