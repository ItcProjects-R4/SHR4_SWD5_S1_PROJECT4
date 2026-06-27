using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Eventify.Domain.Entities;
using EventifyPro.BLL.Services.Interfaces;

namespace EventifyPro.Web.Controllers
{
    [Authorize]
    [Route("Notifications")]
    public class NotificationsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public NotificationsController(
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpPost("MarkAllAsRead")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);

            var referer = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(referer))
            {
                return RedirectToAction("Index", "Home");
            }
            return Redirect(referer);
        }

        [HttpPost("MarkAsRead/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var result = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { success = true });
        }
    }
}
