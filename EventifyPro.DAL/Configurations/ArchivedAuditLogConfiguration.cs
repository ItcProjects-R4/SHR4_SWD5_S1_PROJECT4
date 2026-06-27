namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the ArchivedAuditLog entity.
/// </summary>
public class ArchivedAuditLogConfiguration : IEntityTypeConfiguration<ArchivedAuditLog>
{
    /// <summary>
    /// Configures the ArchivedAuditLog entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<ArchivedAuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TableName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.UserId)
            .HasMaxLength(100);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(100)
            .HasDefaultValue(null);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500)
            .HasDefaultValue(null);

        builder.Property(a => a.ChangedAt)
            .IsRequired();

        builder.Property(a => a.ArchivedAt)
            .IsRequired();
    }
}
