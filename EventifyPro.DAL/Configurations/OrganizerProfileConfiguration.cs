namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the OrganizerProfile entity.
/// </summary>
public class OrganizerProfileConfiguration : IEntityTypeConfiguration<OrganizerProfile>
{
    public void Configure(EntityTypeBuilder<OrganizerProfile> builder)
    {
        builder.HasKey(op => op.Id);

        // One-to-one relationship with ApplicationUser (Owner)
        builder.HasOne(op => op.User)
            .WithOne(u => u.OrganizerProfile)
            .HasForeignKey<OrganizerProfile>(op => op.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Verification relationship with ApplicationUser (Admin)
        builder.HasOne(op => op.VerifiedBy)
            .WithMany()
            .HasForeignKey(op => op.VerifiedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(op => op.UserId)
            .IsRequired();

        builder.Property(op => op.OrganizationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(op => op.Bio)
            .HasMaxLength(1000);

        builder.Property(op => op.LogoUrl)
            .HasMaxLength(500);

        builder.Property(op => op.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(op => op.BusinessPhone)
            .HasMaxLength(30);

        builder.Property(op => op.CommercialRegister)
            .HasMaxLength(100);

        builder.Property(op => op.TaxNumber)
            .HasMaxLength(100);

        builder.Property(op => op.FacebookUrl)
            .HasMaxLength(500);

        builder.Property(op => op.LinkedInUrl)
            .HasMaxLength(500);

        builder.Property(op => op.RejectionReason)
            .HasMaxLength(1000)
            .HasDefaultValue(null);

        builder.Property(op => op.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for fast lookups
        builder.HasIndex(op => op.UserId)
            .IsUnique();
        
        builder.HasIndex(op => op.OrganizationName);
    }
}
