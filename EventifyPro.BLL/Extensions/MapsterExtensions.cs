namespace EventifyPro.BLL.Extensions;

/// <summary>
/// Extension methods for Mapster registration in dependency injection.
/// </summary>
public static class MapsterExtensions
{
    /// <summary>
    /// Adds Mapster mapping configurations to the services collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMapsterMappings(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;

        IRegister[] registers =
        [
            new CategoryMappingRegister(),
            new EventMappingRegister(),
            new TicketTypeMappingRegister(),
            new BookingMappingRegister(),
            new PaymentMappingRegister(),
            new RefundMappingRegister(),
            new ReviewMappingRegister(),
            new ScannerMappingRegister(),
            new TicketMappingRegister(),
            new WaitingListMappingRegister(),
            new UserMappingRegister()
        ];

        foreach (var register in registers)
        {
            register.Register(config);
        }

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
