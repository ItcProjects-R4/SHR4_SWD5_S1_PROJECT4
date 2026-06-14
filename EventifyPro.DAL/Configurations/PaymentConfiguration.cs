namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the Payment entity.
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    /// <summary>
    /// Configures the Payment entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.BookingId)
            .IsRequired();

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Payments_Amount",
                "Amount >= 0");
        });

        builder.Property(p => p.Method)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(p => p.TransactionId)
            .IsRequired(false)
            .HasMaxLength(300)
            .HasColumnType("nvarchar(300)");

        builder.Property(p => p.PaymentDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.HasQueryFilter(p => !p.Booking.Event.IsDeleted);

        // Foreign Key and Relationships
        builder.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: Payment to Refunds
        builder.HasMany(p => p.Refunds)
            .WithOne(r => r.Payment)
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for efficient querying
        builder.HasIndex(p => p.BookingId)
            .HasDatabaseName("IX_Payments_BookingId")
            .IsUnique(true);

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Payments_Status")
            .IsUnique(false);

        builder.HasIndex(p => p.TransactionId)
            .HasDatabaseName("IX_Payments_TransactionId")
            .IsUnique(false);

        builder.HasIndex(p => p.PaymentDate)
            .HasDatabaseName("IX_Payments_PaymentDate")
            .IsUnique(false);

        // Table configuration
        builder.ToTable("Payments", "dbo");
    }
}
