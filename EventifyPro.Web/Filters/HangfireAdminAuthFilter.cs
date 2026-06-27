namespace EventifyPro.Web.Filters
{
    public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // Only allow authenticated users who are in the Admin role
            return httpContext.User.Identity?.IsAuthenticated == true &&
                   httpContext.User.IsInRole(RoleNames.Admin);
        }
    }
}
