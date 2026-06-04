namespace EventifyPro.DAL.AppDatabase;

/// <summary>
/// Main database context for EventifyPro application.
/// Extends IdentityDbContext to support ASP.NET Core Identity with custom user type.
/// </summary>
/// <remarks>
/// This context manages all domain entities and their relationships.
/// It integrates with ASP.NET Core Identity for user management and authentication.
/// All entity configurations are applied through the fluent API in OnModelCreating.
/// </remarks>
public class EventifyDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventifyDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public EventifyDbContext(DbContextOptions<EventifyDbContext> options) : base(options)
    {
    }

    #region DbSets - Core Entities

    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingItem> BookingItems { get; set; }
    public DbSet<TicketType> TicketTypes { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Refund> Refunds { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<WaitingList> WaitingLists { get; set; }
    public DbSet<ScanLog> ScanLogs { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<Feedback> Feedback { get; set; }

    #endregion


    /// <summary>
    /// Configures the model for all entities when the context is created.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to construct the model for this context.</param>
    /// <remarks>
    /// Automatically discovers and applies all entity configurations from the Configurations folder
    /// using reflection. This approach is more maintainable than manually registering each configuration.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically apply all IEntityTypeConfiguration implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventifyDbContext).Assembly);
    }
}
