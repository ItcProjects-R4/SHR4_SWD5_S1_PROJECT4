
namespace EventifyPro.Web.Controllers
{
    [Route("Admin")]
    public class AdminCategoriesController : AdminBaseController
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminCategoriesController> _logger;

        public AdminCategoriesController(
            UserManager<ApplicationUser> userManager,
            IAdminService adminService,
            ILogger<AdminCategoriesController> logger) : base(userManager)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpGet("Categories")]
        public async Task<IActionResult> Categories(CancellationToken cancellationToken)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var result = await _adminService.GetCategoriesAsync(cancellationToken);
                if (result.IsFailure || result.Data == null)
                {
                    TempData["AdminError"] = result.Error ?? "Error loading categories.";
                    return View("~/Views/Admin/Categories.cshtml", new List<Category>());
                }

                return View("~/Views/Admin/Categories.cshtml", result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["AdminError"] = "Error loading categories.";
                return View("~/Views/Admin/Categories.cshtml", new List<Category>());
            }
        }

        [HttpPost("CreateCategory")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> CreateCategory(string name, string description, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["AdminError"] = "Category name is required.";
                return RedirectToAction(nameof(Categories));
            }

            try
            {
                var result = await _adminService.CreateCategoryAsync(name, description, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error creating category.";
                    return RedirectToAction(nameof(Categories));
                }

                TempData["AdminSuccess"] = $"Category '{name}' created successfully.";
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["AdminError"] = "Error creating category.";
                return RedirectToAction(nameof(Categories));
            }
        }

        [HttpPost("UpdateCategory")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> UpdateCategory(int categoryId, string name, string description, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["AdminError"] = "Category name is required.";
                return RedirectToAction(nameof(Categories));
            }

            try
            {
                var result = await _adminService.UpdateCategoryAsync(categoryId, name, description, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error updating category.";
                    return RedirectToAction(nameof(Categories));
                }

                TempData["AdminSuccess"] = $"Category '{name}' updated successfully.";
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                TempData["AdminError"] = "Error updating category.";
                return RedirectToAction(nameof(Categories));
            }
        }

        [HttpPost("DeleteCategory")]
        [ValidateAntiForgeryToken]
        [RateLimit(10, 10)]
        [AuditLog]
        public async Task<IActionResult> DeleteCategory(int categoryId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _adminService.DeleteCategoryAsync(categoryId, cancellationToken);
                if (result.IsFailure)
                {
                    TempData["AdminError"] = result.Error ?? "Error deleting category.";
                    return RedirectToAction(nameof(Categories));
                }

                TempData["AdminSuccess"] = "Category deleted successfully.";
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                TempData["AdminError"] = "Error deleting category.";
                return RedirectToAction(nameof(Categories));
            }
        }
    }
}
