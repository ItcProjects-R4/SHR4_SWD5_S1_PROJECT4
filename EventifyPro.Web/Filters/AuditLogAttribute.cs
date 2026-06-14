using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace EventifyPro.Web.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuditLogAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
            var username = user.Identity?.Name ?? "Anonymous";
            var actionName = context.ActionDescriptor.DisplayName;
            var requestPath = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;

            var logger = context.HttpContext.RequestServices.GetService<ILogger<AuditLogAttribute>>();
            logger?.LogInformation("AUDIT START: User {Username} ({UserId}) is executing action {ActionName} via {Method} at {RequestPath}",
                username, userId, actionName, method, requestPath);

            var executedContext = await next();

            var status = executedContext.Exception == null ? "Success" : "Failed";
            logger?.LogInformation("AUDIT END: Action {ActionName} completed with status: {Status}", actionName, status);

            try
            {
                var webHostEnvironment = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
                var webRootPath = webHostEnvironment?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var logsDir = Path.Combine(webRootPath, "uploads", "logs");
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }

                var logFilePath = Path.Combine(logsDir, "audit_log.json");
                var auditEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    Username = username,
                    Action = actionName,
                    Path = requestPath.ToString(),
                    Method = method,
                    Status = status,
                    Exception = executedContext.Exception?.Message
                };

                var json = JsonSerializer.Serialize(auditEntry) + Environment.NewLine;
                await File.AppendAllTextAsync(logFilePath, json);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to write audit log to file.");
            }
        }
    }
}
