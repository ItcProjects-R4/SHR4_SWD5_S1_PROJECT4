
namespace EventifyPro.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add MVC, MemoryCache, and Mapster
        builder.Services.AddControllersWithViews();
        builder.Services.AddMemoryCache();
        builder.Services.AddSignalR();
        builder.Services.AddWebOptimizer();
        if (string.IsNullOrEmpty(builder.Configuration.GetConnectionString("Redis")))
        {
            builder.Services.AddOutputCache();
        }
        else
        {
            builder.Services.AddStackExchangeRedisOutputCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "EventifyPro_";
            });
        }
        builder.Services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        builder.Services.AddMapsterMappings();

        // Add all configurations and services from the DI extension methods
        builder.Services.AddDatabaseConfiguration(builder.Configuration);
        builder.Services.AddIdentityConfiguration();
        builder.Services.AddApplicationCookieConfiguration();
        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "placeholder-id";
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "placeholder-secret";
                options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            });
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();
        builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<Eventify.Domain.Entities.ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
        builder.Services.AddPaymentServices(builder.Configuration);
        builder.Services.AddApplicationBackgroundServices();
        builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
        builder.Services.AddHangfireConfiguration(builder.Configuration);

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        // Add standard rate limiting middleware
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "text/plain";
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken: token);
            };

            // Policy for OTP verification and generation (5 requests/minute per IP)
            options.AddPolicy("otp-limit", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Policy for ticket bookings (10 requests/minute per IP)
            options.AddPolicy("booking-limit", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Policy for login attempts (20 requests/minute per IP)
            options.AddPolicy("login-limit", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Policy for scanner validation (30 requests/minute per IP)
            options.AddPolicy("scanner-limit", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();

        app.UseForwardedHeaders();

        // Ensure database is created and migrations are applied
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EventifyDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseResponseCompression();
        }

        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        app.UseHttpsRedirection();
        app.UseWebOptimizer();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseRateLimiter(); // Native RateLimiter is placed early to block DDOS attempts
        app.UseAuthentication();
        app.UseMiddleware<UserStatusMiddleware>();
        app.UseAuthorization();
        app.UseMiddleware<MaintenanceModeMiddleware>();
        app.UseOutputCache();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new EventifyPro.Web.Filters.HangfireAdminAuthFilter() }
        });

        // Roles must be seeded first — SeedAdminAsync depends on the Admin role existing.
        await DataSeeder.SeedRolesAsync(app.Services);
        await DataSeeder.SeedAdminAsync(app.Services);
        await DataSeeder.SeedEventsAndCategoriesAsync(app.Services);
        await DataSeeder.SeedSystemSettingsAsync(app.Services);

        // Populate SecuritySettingsCache from database once at startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventifyDbContext>();
            var securityCache = scope.ServiceProvider.GetRequiredService<SecuritySettingsCache>();
            try
            {
                var settings = db.SystemSettings.AsNoTracking().ToList();
                
                var minLengthVal = settings.FirstOrDefault(s => s.Key == "PasswordMinLength")?.Value;
                if (int.TryParse(minLengthVal, out var ml)) securityCache.PasswordMinLength = ml;

                var reqUpperVal = settings.FirstOrDefault(s => s.Key == "RequirePasswordUppercase")?.Value;
                if (bool.TryParse(reqUpperVal, out var ru)) securityCache.RequirePasswordUppercase = ru;

                var reqDigitVal = settings.FirstOrDefault(s => s.Key == "RequirePasswordDigits")?.Value;
                if (bool.TryParse(reqDigitVal, out var rd)) securityCache.RequirePasswordDigits = rd;

                var maxFailedVal = settings.FirstOrDefault(s => s.Key == "MaxFailedLoginsBeforeLockout")?.Value;
                if (int.TryParse(maxFailedVal, out var mf)) securityCache.MaxFailedLoginsBeforeLockout = mf;

                var timeoutVal = settings.FirstOrDefault(s => s.Key == "SessionTimeoutMinutes")?.Value;
                if (int.TryParse(timeoutVal, out var to)) securityCache.SessionTimeoutMinutes = to;
            }
            catch
            {
                // DB not ready or seeded yet, defaults are kept
            }
        }

        // Register Hangfire Recurring Jobs
        using (var scope = app.Services.CreateScope())
        {
            var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            
            // 1. Audit logs archiving: runs daily
            recurringJobs.AddOrUpdate<AuditLogsArchiverJob>(
                "audit-logs-archiving",
                job => job.ArchiveAsync(CancellationToken.None),
                Cron.Daily);

            // 2. Outbox processor safety fallback resilience check: runs every 5 minutes
            recurringJobs.AddOrUpdate<IOutboxService>(
                "outbox-resilience-check",
                service => service.ProcessPendingAsync(CancellationToken.None),
                "*/5 * * * *");
        }

        app.MapHub<EventifyPro.BLL.Hubs.NotificationHub>("/hubs/notification");

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        await app.RunAsync();
    }
}
