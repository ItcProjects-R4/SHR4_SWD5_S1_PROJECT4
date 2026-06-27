namespace EventifyPro.Web.Filters
{
    public class VerifiedOrganizerFilter : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity != null && user.Identity.IsAuthenticated && user.IsInRole(RoleNames.Organizer))
            {
                var actionName = context.ActionDescriptor.RouteValues["action"];
                var controllerName = context.ActionDescriptor.RouteValues["controller"];
                if (string.Equals(controllerName, "Organizer", StringComparison.OrdinalIgnoreCase) && 
                    (string.Equals(actionName, "PendingApproval", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(actionName, "PendingApprovalStatus", StringComparison.OrdinalIgnoreCase)))
                {
                    await next();
                    return;
                }

                var isVerifiedClaim = user.FindFirst("IsVerifiedOrganizer")?.Value;
                bool isVerified = false;
                if (isVerifiedClaim != null && bool.TryParse(isVerifiedClaim, out var claimValue) && claimValue)
                {
                    isVerified = true;
                }
                else
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                    {
                        context.Result = new ChallengeResult();
                        return;
                    }

                    var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
                    isVerified = await unitOfWork.OrganizerProfiles.GetQuery()
                        .AsNoTracking()
                        .Where(p => p.UserId == userId)
                        .Select(p => p.IsVerified)
                        .FirstOrDefaultAsync();
                }

                if (!isVerified)
                {
                    context.Result = new RedirectToActionResult("PendingApproval", "Organizer", null);
                    return;
                }
            }

            await next();
        }
    }
}
