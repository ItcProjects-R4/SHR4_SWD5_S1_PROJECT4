namespace EventifyPro.Web.ViewComponents
{
    public class NotificationCenterViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationCenterViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.UnreadCount = 0;
                return View(new List<NotificationDto>());
            }



            // Fetch unread count
            var unreadResult = await _notificationService.GetUnreadCountAsync(userId, HttpContext.RequestAborted);
            ViewBag.UnreadCount = unreadResult.IsSuccess ? unreadResult.Data : 0;

            // Fetch top 8 notifications
            var notificationsResult = await _notificationService.GetUserNotificationsAsync(userId, 8, HttpContext.RequestAborted);
            var notifications = notificationsResult.IsSuccess && notificationsResult.Data != null 
                ? notificationsResult.Data 
                : new List<NotificationDto>();

            return View(notifications);
        }
    }
}
