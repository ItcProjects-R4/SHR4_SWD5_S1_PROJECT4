using Microsoft.AspNetCore.Mvc;

namespace EventifyPro.Web.Controllers
{
    public class ScannerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
