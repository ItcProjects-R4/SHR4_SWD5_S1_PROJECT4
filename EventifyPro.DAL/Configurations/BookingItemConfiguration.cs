namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the BookingItem entity.
/// </summary>
public class BookingItemConfiguration : IEntityTypeConfiguration<BookingItem>
{
    /// <summary>
    /// Configures the BookingItem entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<BookingItem> builder)
    {
        builder.HasKey(bi => bi.Id);

        builder.Property(bi => bi.Quantity)
            .IsRequired();


        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_BookingItems_Quantity",
                "Quantity > 0");

            t.HasCheckConstraint(
                "CK_BookingItems_UnitPrice",
                "UnitPrice >= 0");
        });

        builder.Property(bi => bi.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.HasQueryFilter(bi => !bi.Booking.Event.IsDeleted && !bi.TicketType.Event.IsDeleted);

        builder.HasOne(bi => bi.Booking)
            .WithMany(b => b.Items)
            .HasForeignKey(bi => bi.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bi => bi.TicketType)
            .WithMany(tt => tt.BookingItems)
            .HasForeignKey(bi => bi.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(bi => new { bi.BookingId, bi.TicketTypeId })
            .IsUnique()
            .HasDatabaseName("IX_BookingItems_BookingId_TicketTypeId");

        builder.HasIndex(bi => bi.BookingId)
            .HasDatabaseName("IX_BookingItems_BookingId");

        builder.HasIndex(bi => bi.TicketTypeId)
            .HasDatabaseName("IX_BookingItems_TicketTypeId");
    }
}
