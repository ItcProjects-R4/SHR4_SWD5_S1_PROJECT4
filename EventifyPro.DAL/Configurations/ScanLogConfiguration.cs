namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the ScanLog entity.
/// Defines append-only audit trail for QR code scans with fraud detection support.
/// </summary>
public class ScanLogConfiguration : IEntityTypeConfiguration<ScanLog>
{
    /// <summary>
    /// Configures the ScanLog entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<ScanLog> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TicketId);

        builder.Property(s => s.EventId)
            .IsRequired();

        builder.Property(s => s.ActualEventId);

        builder.Property(s => s.ScannedById)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(s => s.ScannedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(s => s.Result)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(ScanResult.Valid);

        builder.Property(s => s.RawQRCode)
            .HasMaxLength(500);

        builder.Property(s => s.Notes)
            .HasMaxLength(200);

        builder.HasQueryFilter(s => !s.ScanEvent.IsDeleted);

        // Relationships
        builder.HasOne(s => s.Ticket)
            .WithMany(t => t.ScanLogs)
            .HasForeignKey(s => s.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        // ScanEvent - EventId where scan occurred
        builder.HasOne(s => s.ScanEvent)
            .WithMany(e => e.ScanLogs)
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ScanLogs_Events_ScanEvent");

        // ActualEvent - EventId extracted from QR (for wrong event detection)
        builder.HasOne(s => s.ActualEvent)
            .WithMany(e => e.ActualEventScanLogs)
            .HasForeignKey(s => s.ActualEventId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ScanLogs_Events_ActualEvent");

        builder.HasOne(s => s.Scanner)
            .WithMany(u => u.ScanLogs)
            .HasForeignKey(s => s.ScannedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Indices
        builder.HasIndex(s => new { s.EventId, s.ScannedAt })
            .HasDatabaseName("IX_ScanLogs_EventId_ScannedAt");

        builder.HasIndex(s => new { s.ScannedById, s.ScannedAt })
            .HasDatabaseName("IX_ScanLogs_ScannedById_ScannedAt");

        builder.HasIndex(s => s.TicketId)
            .HasDatabaseName("IX_ScanLogs_TicketId");
    }
}
