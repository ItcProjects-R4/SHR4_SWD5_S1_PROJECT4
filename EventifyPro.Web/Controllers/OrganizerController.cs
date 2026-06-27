
namespace EventifyPro.Web.Controllers
{
    [Authorize(Roles = RoleNames.Organizer)]
    [TypeFilter(typeof(VerifiedOrganizerFilter))]
    public class OrganizerController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Organizer", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> PendingApproval([FromServices] IAuthService authService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var isVerified = await authService.IsOrganizerVerifiedAsync(userId);
                if (isVerified)
                {
                    return RedirectToAction("Organizer", "Dashboard");
                }
            }
            return View();
        }

        [HttpGet("/Organizer/PendingApprovalStatus")]
        public async Task<IActionResult> PendingApprovalStatus([FromServices] IAuthService authService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { isVerified = false });
            }
            
            var isVerified = await authService.IsOrganizerVerifiedAsync(userId);
            return Json(new { isVerified });
        }

        [HttpGet("/OrganizerTickets")]
        public IActionResult OrganizerTickets()
        {
            TempData["OrganizerSuccess"] = "Please select an event to manage its ticket types.";
            return RedirectToAction("OrganizerIndex", "Events");
        }

        [HttpGet("/OrganizerAttendees")]
        public IActionResult OrganizerAttendees()
        {
            TempData["OrganizerSuccess"] = "Please select an event to view its attendees.";
            return RedirectToAction("OrganizerIndex", "Events");
        }
    }
}
