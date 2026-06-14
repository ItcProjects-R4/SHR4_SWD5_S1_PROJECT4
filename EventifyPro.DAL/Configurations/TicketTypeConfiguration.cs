namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the TicketType entity.
/// Defines relationships, constraints, and optimistic concurrency control via RowVersion.
/// </summary>
public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    /// <summary>
    /// Configures the TicketType entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.EventId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Price)
            .IsRequired()
            .HasColumnType("decimal(10,2)")
            .HasDefaultValueSql("0.00");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_TicketTypes_Price",
                "Price >= 0");
        });

        builder.Property(t => t.TotalQuantity)
            .IsRequired();

        builder.Property(t => t.SoldQuantity)
            .IsRequired()
            .HasDefaultValueSql("0");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_TicketTypes_TotalQuantity",
                "TotalQuantity > 0");
            t.HasCheckConstraint(
                "CK_TicketTypes_SoldQuantity",
                "SoldQuantity >= 0 AND SoldQuantity <= TotalQuantity");
        });

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.SaleStartDate);

        builder.Property(t => t.SaleEndDate);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_TicketTypes_SaleEndDate",
                "SaleEndDate IS NULL OR SaleStartDate IS NULL OR SaleEndDate > SaleStartDate");
        });

        builder.Property(t => t.RowVersion)
            .IsRowVersion();

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValue(null);

        builder.HasQueryFilter(t => !t.Event.IsDeleted);

        // Relationships
        builder.HasOne(t => t.Event)
            .WithMany(e => e.TicketTypes)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indices
        builder.HasIndex(t => new { t.EventId, t.Name })
            .IsUnique()
            .HasDatabaseName("IX_TicketTypes_EventId_Name");

        builder.HasIndex(t => t.EventId)
            .HasDatabaseName("IX_TicketTypes_EventId");
    }
}
