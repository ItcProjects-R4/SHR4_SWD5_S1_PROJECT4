namespace EventifyPro.Web.Middleware
{
    /// <summary>
    /// Global exception handling middleware that catches all unhandled exceptions,
    /// logs them with a unique correlation ID, and returns a friendly error page or JSON payload.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate unique correlation ID for tracking
            var correlationId = Guid.NewGuid().ToString("D");
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            context.Items["CorrelationId"] = correlationId;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. Path: {Path}, Method: {Method}, Correlation ID: {CorrelationId}", 
                    context.Request.Path, context.Request.Method, correlationId);

                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            // If response has already started, we can't write to it
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The response has already started, the global exception middleware will not write the exception to response.");
                return;
            }

            var isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         context.Request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (isAjax)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var responsePayload = new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "A critical error occurred while processing your request. Please try again or contact support.",
                    CorrelationId = correlationId,
                    Details = _env.IsDevelopment() ? exception.ToString() : null
                };

                var json = JsonSerializer.Serialize(responsePayload);
                await context.Response.WriteAsync(json);
            }
            else
            {
                // Redirect standard page request to custom 500 error page, carrying correlationId in query string
                context.Response.Redirect($"/Error/500?correlationId={correlationId}");
                await Task.CompletedTask;
            }
        }
    }
}
