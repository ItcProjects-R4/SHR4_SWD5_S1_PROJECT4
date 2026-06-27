namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the PayoutRequest entity.
/// </summary>
public class PayoutRequestConfiguration : IEntityTypeConfiguration<PayoutRequest>
{
    public void Configure(EntityTypeBuilder<PayoutRequest> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Pending");

        builder.Property(p => p.Method)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("Bank Transfer");

        builder.Property(p => p.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.RequestedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(p => p.Organizer)
            .WithMany()
            .HasForeignKey(p => p.OrganizerId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PayoutRequests_AspNetUsers_OrganizerId");
    }
}
