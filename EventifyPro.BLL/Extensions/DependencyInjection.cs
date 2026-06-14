using DinkToPdf;
using DinkToPdf.Contracts;

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
            })
            .AddEntityFrameworkStores<EventifyDbContext>()
            .AddDefaultTokenProviders();

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
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPdfService, PdfService>();

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
