using EventifyPro.DAL.Seeders;
namespace EventifyPro.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add MVC, MemoryCache, and Mapster
        builder.Services.AddControllersWithViews();
        builder.Services.AddMemoryCache();
        builder.Services.AddMapsterMappings();

        // Add all configurations and services from the DI extension methods
        builder.Services.AddDatabaseConfiguration(builder.Configuration);
        builder.Services.AddIdentityConfiguration();
        builder.Services.AddApplicationCookieConfiguration();
        builder.Services.AddRepositories();
        builder.Services.AddApplicationServices();
        builder.Services.AddPaymentServices(builder.Configuration);
        builder.Services.AddApplicationBackgroundServices();

        var app = builder.Build();

        // Ensure database is created and migrations are applied
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EventifyDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        // Roles must be seeded first — SeedAdminAsync depends on the Admin role existing.
        await DataSeeder.SeedRolesAsync(app.Services);
        await DataSeeder.SeedAdminAsync(app.Services);
        await DataSeeder.SeedEventsAndCategoriesAsync(app.Services);

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        await app.RunAsync();
    }
}
