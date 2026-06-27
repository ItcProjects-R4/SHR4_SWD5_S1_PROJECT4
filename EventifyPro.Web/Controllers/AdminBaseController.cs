
namespace EventifyPro.Web.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    [TypeFilter(typeof(AdminIpWhitelistFilter))]
    [Route("Admin")]
    public abstract class AdminBaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;

        protected AdminBaseController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected async Task PopulateAdminViewDataAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = user?.FullName ?? "Admin User";
            ViewBag.AdminAvatar = user?.ProfileImageUrl;
        }
    }
}
