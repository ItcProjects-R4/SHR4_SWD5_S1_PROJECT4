namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the WaitingList entity.
/// Manages queue entries with status tracking and expiry management.
/// </summary>
public class WaitingListConfiguration : IEntityTypeConfiguration<WaitingList>
{
    /// <summary>
    /// Configures the WaitingList entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<WaitingList> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.EventId)
            .IsRequired();

        builder.Property(w => w.TicketTypeId)
            .IsRequired();

        builder.Property(w => w.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(w => w.QuantityWanted)
            .IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_WaitingList_QuantityWanted",
                "QuantityWanted > 0");
        });

        builder.Property(w => w.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(WaitingListStatus.Waiting)
            .HasSentinel((WaitingListStatus)255);

        builder.Property(w => w.JoinedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(w => w.NotifiedAt);

        builder.Property(w => w.ExpiresAt);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_WaitingList_ExpiresAt",
                "NotifiedAt IS NULL OR ExpiresAt IS NULL OR ExpiresAt > NotifiedAt");
        });

        builder.HasQueryFilter(w => !w.Event.IsDeleted);

        // Relationships
        // EventId with Restrict to prevent multiple cascade paths
        builder.HasOne(w => w.Event)
            .WithMany(e => e.WaitingListEntries)
            .HasForeignKey(w => w.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // TicketTypeId with Cascade (tickets for this type cascade delete waiting entries)
        builder.HasOne(w => w.TicketType)
            .WithMany(tt => tt.WaitingListEntries)
            .HasForeignKey(w => w.TicketTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indices
        builder.HasIndex(w => new { w.TicketTypeId, w.Status, w.JoinedAt })
            .HasDatabaseName("IX_WaitingList_TT_Status_JoinedAt");

        builder.HasIndex(w => new { w.UserId, w.TicketTypeId })
            .IsUnique()
            .HasDatabaseName("IX_WaitingList_UserId_TicketTypeId");
    }
}
