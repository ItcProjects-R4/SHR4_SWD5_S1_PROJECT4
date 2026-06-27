namespace EventifyPro.Web.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IEventService _eventService;

        public CategoriesController(ICategoryService categoryService, IEventService eventService)
        {
            _categoryService = categoryService;
            _eventService = eventService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var result = await _categoryService.GetAllAsync(cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                return View(new List<CategoryViewModel>());
            }

            var viewModels = result.Data.Adapt<List<CategoryViewModel>>();
            return View(viewModels);
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
            var category = categoriesResult.IsSuccess && categoriesResult.Data != null
                ? categoriesResult.Data.FirstOrDefault(c => c.Id == id)
                : null;

            if (category == null)
            {
                return NotFound();
            }

            var filter = new EventFilterDto
            {
                CategoryId = id,
                Status = "Published",
                PageNumber = 1,
                PageSize = 50
            };

            var eventsResult = await _eventService.SearchAsync(filter, cancellationToken);
            
            ViewBag.CategoryId = category.Id;
            ViewBag.CategoryName = category.Name;
            ViewBag.CategoryDescription = category.Description;

            var eventsVm = eventsResult.Data.Adapt<List<EventSummaryViewModel>>();
            return View(eventsVm);
        }

        // === Admin Category Management Actions ===

        // GET: Categories/Create
        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Create()
        {
            return View(new CategoryFormViewModel());
        }

        // POST: Categories/Create
        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = new CategoryCreateDto
            {
                Name = System.Net.WebUtility.HtmlEncode(model.Name?.Trim() ?? string.Empty),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : System.Net.WebUtility.HtmlEncode(model.Description.Trim())
            };

            var result = await _categoryService.CreateAsync(dto, cancellationToken);
            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create category.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Edit/5
        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var categoriesResult = await _categoryService.GetAllAsync(cancellationToken);
            var category = categoriesResult.IsSuccess && categoriesResult.Data != null
                ? categoriesResult.Data.FirstOrDefault(c => c.Id == id)
                : null;

            if (category == null)
            {
                return NotFound("Category not found.");
            }

            var model = new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return View(model);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.Id)
            {
                return BadRequest("ID mismatch.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = new CategoryUpdateDto
            {
                Id = model.Id,
                Name = System.Net.WebUtility.HtmlEncode(model.Name?.Trim() ?? string.Empty),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : System.Net.WebUtility.HtmlEncode(model.Description.Trim())
            };

            var result = await _categoryService.UpdateAsync(id, dto, cancellationToken);
            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update category.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        [AuditLog]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _categoryService.DeleteAsync(id, cancellationToken);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to delete category.";
            }
            else
            {
                TempData["SuccessMessage"] = "Category deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/CheckDuplicateName
        [HttpGet]
        public async Task<IActionResult> CheckDuplicateName(string name, int? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(true);
            }

            var nameTrimmed = name.Trim();
            var exists = await _categoryService.ExistsByNameAsync(nameTrimmed, id, cancellationToken);

            return Json(!exists);
        }
    }
}
