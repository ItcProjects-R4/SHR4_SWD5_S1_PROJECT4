namespace EventifyPro.Web.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _limit;
        private readonly int _seconds;

        public RateLimitAttribute(int limit, int seconds)
        {
            _limit = limit;
            _seconds = seconds;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            if (cache == null)
            {
                base.OnActionExecuting(context);
                return;
            }

            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var actionName = context.ActionDescriptor.RouteValues["action"] ?? "action";

            var cacheKey = $"RateLimit_{ipAddress}_{userId}_{actionName}";

            if (cache.TryGetValue(cacheKey, out int requestCount))
            {
                if (requestCount >= _limit)
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = 429,
                        Content = $"Too many requests. Please wait {_seconds} seconds before trying again."
                    };
                    return;
                }

                cache.Set(cacheKey, requestCount + 1, TimeSpan.FromSeconds(_seconds));
            }
            else
            {
                cache.Set(cacheKey, 1, TimeSpan.FromSeconds(_seconds));
            }

            base.OnActionExecuting(context);
        }
    }
}
