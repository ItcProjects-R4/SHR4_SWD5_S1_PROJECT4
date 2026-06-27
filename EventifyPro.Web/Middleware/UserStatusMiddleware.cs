namespace EventifyPro.Web.Middleware
{
    public class UserStatusMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private const string CachePrefix = "UserStatus_";

        public UserStatusMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, EventifyDbContext dbContext, SignInManager<ApplicationUser> signInManager)
        {
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var cacheKey = CachePrefix + userId;

                    if (!_cache.TryGetValue(cacheKey, out bool isActive))
                    {
                        var user = await dbContext.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == userId);

                        isActive = user != null && user.IsActive;

                        // Cache the status for 60 seconds
                        _cache.Set(cacheKey, isActive, TimeSpan.FromSeconds(60));
                    }

                    if (!isActive)
                    {
                        // User is deactivated! Force log out.
                        await signInManager.SignOutAsync();
                        
                        // Redirect
                        context.Response.Redirect("/Account/Login?errorMessage=Your account has been deactivated.");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
