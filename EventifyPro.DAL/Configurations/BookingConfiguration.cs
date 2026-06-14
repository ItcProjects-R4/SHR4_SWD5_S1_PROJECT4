namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the Booking entity.
/// </summary>
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    /// <summary>
    /// Configures the Booking entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(10,2)")
            .HasDefaultValueSql("0.00");

        builder.Property(b => b.ServiceFee)
            .IsRequired()
            .HasColumnType("decimal(10,2)")
            .HasDefaultValueSql("0.00");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Bookings_TotalAmount",
                "TotalAmount >= 0");
            t.HasCheckConstraint(
                "CK_Bookings_ServiceFee",
                "ServiceFee >= 0");
        });

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(BookingStatus.Pending)
            .HasSentinel((BookingStatus)255);

        builder.Property(b => b.BookingReference)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.BookingDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(b => b.UpdatedAt)
            .HasDefaultValue(null);

        builder.HasQueryFilter(b => !b.Event.IsDeleted);

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.BookingReference)
            .IsUnique()
            .HasDatabaseName("IX_Bookings_BookingReference");

        builder.HasIndex(b => new { b.UserId, b.Status })
            .HasDatabaseName("IX_Bookings_UserId_Status");

        builder.HasIndex(b => b.EventId)
            .HasDatabaseName("IX_Bookings_EventId");
    }
}
