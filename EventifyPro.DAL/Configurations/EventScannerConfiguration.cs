namespace EventifyPro.DAL.Configurations;

public class EventScannerConfiguration : IEntityTypeConfiguration<EventScanner>
{
    public void Configure(EntityTypeBuilder<EventScanner> builder)
    {
        builder.ToTable("EventScanners");

        builder.HasKey(es => es.Id);

        builder.Property(es => es.ScannerId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(es => es.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(es => new { es.ScannerId, es.EventId })
            .IsUnique()
            .HasDatabaseName("IX_EventScanners_ScannerId_EventId");

        builder.HasOne(es => es.Scanner)
            .WithMany(u => u.AssignedScannerEvents)
            .HasForeignKey(es => es.ScannerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(es => es.Event)
            .WithMany(e => e.AssignedScanners)
            .HasForeignKey(es => es.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(es => !es.Event.IsDeleted);
    }
}
