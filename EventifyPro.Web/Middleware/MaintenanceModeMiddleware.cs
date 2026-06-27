namespace EventifyPro.Web.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceModeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ISystemSettingService systemSettingService)
        {
            var isMaintenanceEnabled = await systemSettingService.GetSettingValueAsync<bool>("EnableMaintenanceMode", false);

            if (isMaintenanceEnabled)
            {
                var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

                // Allow admin pages, login/logout, static files, and payment webhooks to bypass maintenance mode
                var isBypassPath = path.StartsWith("/admin") ||
                                   path.StartsWith("/account/login") ||
                                   path.StartsWith("/account/logout") ||
                                   path.StartsWith("/dashboard/admin") ||
                                   path.StartsWith("/api/payment/callback") ||
                                   path.StartsWith("/uploads") ||
                                   path.StartsWith("/lib") ||
                                   path.StartsWith("/css") ||
                                   path.StartsWith("/js") ||
                                   path.Contains("favicon") ||
                                   context.User.IsInRole(RoleNames.Admin);

                if (!isBypassPath)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "text/html; charset=utf-8";

                    var maintenanceHtml = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Site Under Maintenance - Eventify Pro</title>
    <link href=""https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;800&display=swap"" rel=""stylesheet"">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Poppins', sans-serif;
            background: #ede9fe;
            background: linear-gradient(135deg, #ede9fe 0%, #ddd6fe 100%);
            color: #1f1b2e;
            height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 24px;
            text-align: center;
        }
        .card {
            background: white;
            padding: 48px;
            border-radius: 24px;
            box-shadow: 0 20px 60px rgba(91, 33, 182, 0.15);
            max-width: 500px;
            width: 100%;
            border: 1px solid rgba(255, 255, 255, 0.8);
        }
        .icon {
            font-size: 64px;
            color: #7c3aed;
            margin-bottom: 24px;
            animation: pulse 2s infinite;
        }
        h1 {
            font-size: 28px;
            font-weight: 800;
            color: #4c1d95;
            margin-bottom: 12px;
        }
        p {
            font-size: 15px;
            color: #6b7280;
            line-height: 1.6;
            margin-bottom: 24px;
        }
        .badge {
            display: inline-block;
            padding: 6px 16px;
            background: rgba(124, 58, 237, 0.1);
            color: #7c3aed;
            border-radius: 50px;
            font-size: 13px;
            font-weight: 600;
        }
        @keyframes pulse {
            0% { transform: scale(1); }
            50% { transform: scale(1.1); }
            100% { transform: scale(1); }
        }
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""icon"">⚙️</div>
        <h1>Under Maintenance</h1>
        <p>We are currently performing scheduled system upgrades to Eventify Pro to improve your experience. We will be back online shortly.</p>
        <span class=""badge"">Planned Maintenance Mode</span>
    </div>
</body>
</html>";
                    await context.Response.WriteAsync(maintenanceHtml);
                    return;
                }
            }

            await _next(context);
        }
    }
}
