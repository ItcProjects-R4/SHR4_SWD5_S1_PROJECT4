namespace EventifyPro.Web.Extensions;

/// <summary>
/// Extension methods for configuring Hangfire in the presentation layer.
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// Configures Hangfire storage and background server, and registers the Outbox Service Decorator.
    /// </summary>
    public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection connection string is missing.");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true // Highly recommended for SQL Server storage performance
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Max(2, Environment.ProcessorCount * 2);
        });

        // Decorate the registered IOutboxService with HangfireOutboxServiceDecorator
        var outboxDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOutboxService));
        if (outboxDescriptor != null)
        {
            services.Remove(outboxDescriptor);
            services.AddScoped<OutboxService>(); // Register original concrete implementation
            
            // Register decorator
            services.AddScoped<IOutboxService>(provider => 
                new HangfireOutboxServiceDecorator(
                    provider.GetRequiredService<OutboxService>(),
                    provider.GetRequiredService<IBackgroundJobClient>()
                ));
        }

        return services;
    }
}
