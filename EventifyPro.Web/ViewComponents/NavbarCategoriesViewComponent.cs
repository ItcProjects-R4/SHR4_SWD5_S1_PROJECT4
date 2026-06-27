namespace EventifyPro.Web.ViewComponents
{
    public class NavbarCategoriesViewComponent : ViewComponent
    {
        private readonly ICategoryService _categoryService;

        public NavbarCategoriesViewComponent(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var result = await _categoryService.GetAllAsync(HttpContext.RequestAborted);
            if (result.IsSuccess && result.Data != null)
            {
                // We can order them alphabetically or limit to top 5
                var categories = result.Data.OrderBy(c => c.Name).ToList();
                return View(categories);
            }

            return View(new System.Collections.Generic.List<EventifyPro.BLL.DTOs.Category.CategoryDto>());
        }
    }
}
