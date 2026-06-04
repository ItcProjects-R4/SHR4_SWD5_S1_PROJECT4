using Microsoft.AspNetCore.Mvc;

namespace EventifyPro.Web.Controllers
{
    public class OrganizerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
