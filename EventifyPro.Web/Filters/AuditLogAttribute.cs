namespace EventifyPro.Web.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuditLogAttribute : ActionFilterAttribute
    {
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
            var username = user.Identity?.Name ?? "Anonymous";
            var actionName = context.ActionDescriptor.DisplayName;
            var requestPath = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;

            var logger = context.HttpContext.RequestServices.GetService<ILogger<AuditLogAttribute>>();
            logger?.LogInformation("AUDIT: User {Username} executing {ActionName}", username, actionName);

            var executedContext = await next();

            var status = executedContext.Exception == null ? "Success" : "Failed";

            // 1. Log to Database
            var dbContext = context.HttpContext.RequestServices.GetService<EventifyDbContext>();
            if (dbContext != null)
            {
                try
                {
                    string actionParams = string.Empty;
                    try
                    {
                        actionParams = JsonSerializer.Serialize(context.ActionArguments);
                    }
                    catch (Exception)
                    {
                        actionParams = "Could not serialize parameters.";
                    }

                    var dbAuditLog = new AuditLog
                    {
                        TableName = context.Controller.GetType().Name,
                        Action = context.ActionDescriptor.RouteValues["action"] ?? "Unknown",
                        EntityId = string.Empty,
                        OldValues = actionParams,
                        NewValues = executedContext.Exception != null ? $"Error: {executedContext.Exception.Message}" : "Success",
                        UserId = userId != "Anonymous" ? userId : null,
                        IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                        ChangedAt = DateTime.UtcNow
                    };

                    if (context.ActionArguments.TryGetValue("userId", out var targetId) && targetId != null)
                    {
                        dbAuditLog.EntityId = targetId.ToString() ?? string.Empty;
                    }
                    else if (context.ActionArguments.TryGetValue("id", out var idVal) && idVal != null)
                    {
                        dbAuditLog.EntityId = idVal.ToString() ?? string.Empty;
                    }

                    await dbContext.AuditLogs.AddAsync(dbAuditLog);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to write audit log to database.");
                }
            }

            // 2. Log to Flat File
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

            await _fileLock.WaitAsync();
            try
            {
                var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "logs");
                Directory.CreateDirectory(logsDir);
                var logFilePath = Path.Combine(logsDir, $"audit_{DateTime.UtcNow:yyyyMMdd}.log");
                await File.AppendAllTextAsync(logFilePath, json);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to write audit log to file.");
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
