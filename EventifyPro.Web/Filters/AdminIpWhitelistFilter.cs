namespace EventifyPro.Web.Filters
{
    public class AdminIpWhitelistFilter : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var systemSettingService = context.HttpContext.RequestServices.GetRequiredService<ISystemSettingService>();
            var whitelistString = await systemSettingService.GetSettingValueAsync("AdminIpWhitelist", "*");

            if (whitelistString != "*")
            {
                var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
                
                if (remoteIp != null)
                {
                    var clientIp = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4().ToString() : remoteIp.ToString();
                    var ipList = whitelistString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ip => ip.Trim());

                    // Resolve loopback address
                    if (IPAddress.IsLoopback(remoteIp))
                    {
                        // Check if the loopback address is matched in whitelist
                        var hasLoopbackMatch = ipList.Contains("127.0.0.1") || ipList.Contains("::1") || ipList.Contains("localhost") || ipList.Contains(clientIp);
                        if (!hasLoopbackMatch)
                        {
                            context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
                            return;
                        }
                    }
                    else
                    {
                        var isIpAllowed = ipList.Contains(clientIp);

                        if (!isIpAllowed)
                        {
                            context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
                            return;
                        }
                    }
                }
                else
                {
                    // If remote IP is null, block access for safety when whitelist is active
                    context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
                    return;
                }
            }

            await next();
        }
    }
}
