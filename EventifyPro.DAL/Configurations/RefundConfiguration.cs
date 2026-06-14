namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the Refund entity.
/// </summary>
public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    /// <summary>
    /// Configures the Refund entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        // Primary Key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.PaymentId)
            .IsRequired();

        builder.Property(r => r.BookingId)
            .IsRequired();

        builder.Property(r => r.Amount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(RefundStatus.Pending);

        builder.Property(r => r.TransactionId)
            .IsRequired(false)
            .HasMaxLength(300)
            .HasColumnType("nvarchar(300)");

        builder.Property(r => r.Reason)
            .IsRequired(false)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(r => r.InitiatedById)
            .IsRequired()
            .HasMaxLength(450)
            .HasColumnType("nvarchar(450)");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(r => r.ProcessedAt)
            .IsRequired(false);

        builder.HasQueryFilter(r => !r.Booking.Event.IsDeleted && !r.Payment.Booking.Event.IsDeleted);

        // Foreign Keys and Relationships
        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Booking)
            .WithMany(b => b.Refunds)
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Initiator)
            .WithMany(u => u.InitiatedRefunds)
            .HasForeignKey(r => r.InitiatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for efficient querying
        builder.HasIndex(r => r.PaymentId)
            .HasDatabaseName("IX_Refunds_PaymentId")
            .IsUnique(false);

        builder.HasIndex(r => r.BookingId)
            .HasDatabaseName("IX_Refunds_BookingId")
            .IsUnique(false);

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Refunds_Status")
            .IsUnique(false);

        builder.HasIndex(r => r.InitiatedById)
            .HasDatabaseName("IX_Refunds_InitiatedById")
            .IsUnique(false);

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Refunds_CreatedAt")
            .IsUnique(false);

        builder.HasIndex(r => new { r.PaymentId, r.Status })
            .HasDatabaseName("IX_Refunds_PaymentId_Status")
            .IsUnique(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Refund_Amount_Positive",
                "[Amount] > 0");
        });

        // Table configuration
        builder.ToTable("Refunds", "dbo");
    }
}
