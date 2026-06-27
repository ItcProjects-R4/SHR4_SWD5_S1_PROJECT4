namespace EventifyPro.BLL.Extensions;

public static class DependencyInjection
{
    /// <summary>
    /// Adds database context configuration
    /// </summary>
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EventifyDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    /// <summary>
    /// Adds Identity configuration
    /// </summary>
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<SecuritySettingsCache>();

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<EventifyDbContext>()
            .AddDefaultTokenProviders();

        services.AddTransient<IConfigureOptions<IdentityOptions>, DynamicIdentityOptionsConfiguration>();

        return services;
    }

    /// <summary>
    /// Adds application cookie configuration
    /// </summary>
    public static IServiceCollection AddApplicationCookieConfiguration(this IServiceCollection services)
    {
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        services.AddTransient<IConfigureOptions<CookieAuthenticationOptions>, DynamicCookieOptionsConfiguration>();

        return services;
    }

    /// <summary>
    /// Adds repository and unit of work registrations
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds all application business logic services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddValidatorsFromAssemblyContaining<CategoryCreateDtoValidator>();

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ITicketTypeService, TicketTypeService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IScanLogService, ScanLogService>();
        services.AddScoped<IWaitingListService, WaitingListService>();
        services.AddScoped<IQRService, QRService>();
        services.AddScoped<IUploadHelper, UploadHelper>();
        services.AddScoped<IImageUploadService, ImageUploadService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ISavedEventService, SavedEventService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<IAdminService, AdminService>();

        // Register new BLL services
        services.AddScoped<IDistributedLockService, DistributedLockService>();
        services.AddScoped<IHomeService, HomeService>();
        services.AddScoped<IPayoutService, PayoutService>();
        services.AddScoped<IOrganizerScannersService, OrganizerScannersService>();
        services.AddHttpClient<IAiService, GeminiAiService>();

        services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

        return services;
    }

    /// <summary>
    /// Adds payment gateway configuration and services
    /// </summary>
    public static IServiceCollection AddPaymentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PaymobSettings>(
            configuration.GetSection(PaymobSettings.SectionName));
        services.AddHttpClient<IPaymentService, PaymobPaymentService>();
        services.AddScoped<IRefundService, RefundService>();

        return services;
    }
}

public class DynamicIdentityOptionsConfiguration : IConfigureOptions<IdentityOptions>
{
    private readonly SecuritySettingsCache _cache;

    public DynamicIdentityOptionsConfiguration(SecuritySettingsCache cache)
    {
        _cache = cache;
    }

    public void Configure(IdentityOptions options)
    {
        options.Password.RequiredLength = _cache.PasswordMinLength;
        options.Password.RequireUppercase = _cache.RequirePasswordUppercase;
        options.Password.RequireDigit = _cache.RequirePasswordDigits;
        options.Lockout.MaxFailedAccessAttempts = _cache.MaxFailedLoginsBeforeLockout;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    }
}

public class DynamicCookieOptionsConfiguration : IConfigureNamedOptions<CookieAuthenticationOptions>
{
    private readonly SecuritySettingsCache _cache;

    public DynamicCookieOptionsConfiguration(SecuritySettingsCache cache)
    {
        _cache = cache;
    }

    public void Configure(string? name, CookieAuthenticationOptions options)
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(_cache.SessionTimeoutMinutes);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    }

    public void Configure(CookieAuthenticationOptions options) => Configure(Options.DefaultName, options);
}

public class SecuritySettingsCache
{
    public int PasswordMinLength { get; set; } = 8;
    public bool RequirePasswordUppercase { get; set; } = true;
    public bool RequirePasswordDigits { get; set; } = true;
    public int MaxFailedLoginsBeforeLockout { get; set; } = 5;
    public int SessionTimeoutMinutes { get; set; } = 30;
}
