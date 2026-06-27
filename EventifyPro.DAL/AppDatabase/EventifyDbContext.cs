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
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventifyDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public EventifyDbContext(
        DbContextOptions<EventifyDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    #region DbSets - Core Entities

    public DbSet<Event> Events { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ArchivedAuditLog> ArchivedAuditLogs { get; set; }
    public DbSet<OrganizerProfile> OrganizerProfiles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingItem> BookingItems { get; set; }
    public DbSet<TicketType> TicketTypes { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Refund> Refunds { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<SavedEvent> SavedEvents { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Feedback> Feedback { get; set; }
    public DbSet<WaitingList> WaitingLists { get; set; }
    public DbSet<ScanLog> ScanLogs { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<PayoutRequest> PayoutRequests { get; set; }
    public DbSet<EventScanner> EventScanners { get; set; }

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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        string? userId = null;
        try
        {
            userId = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception)
        {
            // Avoid failure in non-request contexts
        }

        UpdateAuditProperties();

        var auditEntries = OnBeforeSaveChanges(userId);

        var result = await base.SaveChangesAsync(cancellationToken);

        await OnAfterSaveChangesAsync(auditEntries, cancellationToken);

        return result;
    }

    public override int SaveChanges()
    {
        string? userId = null;
        try
        {
            userId = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception)
        {
            // Avoid failure in non-request contexts
        }

        UpdateAuditProperties();

        var auditEntries = OnBeforeSaveChanges(userId);

        var result = base.SaveChanges();

        OnAfterSaveChanges(auditEntries);

        return result;
    }

    private void UpdateAuditProperties()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditable)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProp != null && (createdAtProp.CurrentValue == null || (DateTime)createdAtProp.CurrentValue == default))
                {
                    createdAtProp.CurrentValue = DateTime.UtcNow;
                }
                var bookingDateProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "BookingDate");
                if (bookingDateProp != null && (bookingDateProp.CurrentValue == null || (DateTime)bookingDateProp.CurrentValue == default))
                {
                    bookingDateProp.CurrentValue = DateTime.UtcNow;
                }
                var paymentDateProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "PaymentDate");
                if (paymentDateProp != null && (paymentDateProp.CurrentValue == null || (DateTime)paymentDateProp.CurrentValue == default))
                {
                    paymentDateProp.CurrentValue = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null)
                {
                    updatedAtProp.CurrentValue = DateTime.UtcNow;
                }
            }
        }
    }

    private List<AuditEntry> OnBeforeSaveChanges(string? userId)
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.Entity is not IAuditable || entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Metadata.DisplayName(),
                UserId = userId,
                Action = entry.State switch
                {
                    EntityState.Added => "Insert",
                    EntityState.Deleted => "Delete",
                    EntityState.Modified => "Update",
                    _ => entry.State.ToString()
                }
            };

            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    if (property.IsTemporary)
                    {
                        auditEntry.TemporaryProperties.Add(property);
                    }
                    else
                    {
                        auditEntry.EntityId = property.CurrentValue?.ToString() ?? string.Empty;
                    }
                    continue;
                }

                string propertyName = property.Metadata.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        if (property.IsTemporary)
                        {
                            auditEntry.TemporaryProperties.Add(property);
                        }
                        else
                        {
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            var original = property.OriginalValue;
                            var current = property.CurrentValue;
                            if (!Equals(original, current))
                            {
                                auditEntry.OldValues[propertyName] = original;
                                auditEntry.NewValues[propertyName] = current;
                            }
                        }
                        break;
                }
            }
        }

        return auditEntries;
    }

    private (string? IpAddress, string? UserAgent) GetAuditRequestDetails()
    {
        string? ipAddress = null;
        string? userAgent = null;
        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                ipAddress = httpContext.Connection?.RemoteIpAddress?.ToString();
                userAgent = httpContext.Request?.Headers["User-Agent"].ToString();
            }
        }
        catch (Exception) { }
        return (ipAddress, userAgent);
    }

    private async Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        if (auditEntries == null || auditEntries.Count == 0)
        {
            return;
        }

        var requestDetails = GetAuditRequestDetails();

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.EntityId = prop.CurrentValue?.ToString() ?? string.Empty;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            var auditLog = auditEntry.ToAuditLog(requestDetails.IpAddress, requestDetails.UserAgent);
            await Set<AuditLog>().AddAsync(auditLog, cancellationToken);
        }

        await base.SaveChangesAsync(cancellationToken);
    }

    private void OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
        {
            return;
        }

        var requestDetails = GetAuditRequestDetails();

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.EntityId = prop.CurrentValue?.ToString() ?? string.Empty;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            var auditLog = auditEntry.ToAuditLog(requestDetails.IpAddress, requestDetails.UserAgent);
            Set<AuditLog>().Add(auditLog);
        }

        base.SaveChanges();
    }

    [DbFunction("JSON_VALUE", IsBuiltIn = true, IsNullable = true)]
    public static string? JsonValue(string? expression, string path) => throw new NotSupportedException();
}

public class AuditEntry
{
    public EntityEntry Entry { get; }
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();

    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public bool HasTemporaryProperties => TemporaryProperties.Count > 0;

    public AuditLog ToAuditLog(string? ipAddress = null, string? userAgent = null)
    {
        return new AuditLog
        {
            TableName = TableName,
            Action = Action,
            EntityId = EntityId,
            UserId = UserId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ChangedAt = DateTime.UtcNow,
            OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues)
        };
    }
}
