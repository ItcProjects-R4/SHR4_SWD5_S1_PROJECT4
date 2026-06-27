namespace EventifyPro.Web.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode, [FromQuery] string? correlationId = null)
        {
            ViewBag.CorrelationId = correlationId ?? HttpContext.Items["CorrelationId"] as string ?? HttpContext.TraceIdentifier;

            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Sorry, the page you requested could not be found.";
                    return View("NotFound");
                case 403:
                    ViewBag.ErrorMessage = "Sorry, you do not have permission to access this resource.";
                    return View("AccessDenied");
                default:
                    ViewBag.ErrorMessage = "Sorry, something went wrong on our server.";
                    return View("ServerError");
            }
        }
    }
}
