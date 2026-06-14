namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the Ticket entity.
/// Defines relationships, QR code validation, and usage tracking constraints.
/// </summary>
public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    /// <summary>
    /// Configures the Ticket entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.EventId)
            .IsRequired();

        builder.Property(t => t.BookingId)
            .IsRequired();

        builder.Property(t => t.TicketTypeId)
            .IsRequired();

        builder.Property(t => t.QRCode)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.UsedAt);

        builder.Property(t => t.ScannedById)
            .HasMaxLength(450);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasQueryFilter(t => !t.Event.IsDeleted);

        // Relationships
        builder.HasOne(t => t.Event)
            .WithMany(e => e.Tickets)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Booking)
            .WithMany(b => b.Tickets)
            .HasForeignKey(t => t.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.TicketType)
            .WithMany(tt => tt.Tickets)
            .HasForeignKey(t => t.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Scanner)
            .WithMany()
            .HasForeignKey(t => t.ScannedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Indices
        builder.HasIndex(t => t.QRCode)
            .IsUnique()
            .HasDatabaseName("IX_Tickets_QRCode");

        builder.HasIndex(t => new { t.EventId, t.IsUsed })
            .HasDatabaseName("IX_Tickets_EventId_IsUsed");
    }
}
