namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for footer feedback messages.
/// </summary>
public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasMaxLength(100);

        builder.Property(f => f.Email)
            .HasMaxLength(256);

        builder.Property(f => f.Message)
            .IsRequired()
            .HasMaxLength(1500);

        builder.Property(f => f.IsApproved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(f => f.ApprovedAt)
            .HasDefaultValue(null);

        builder.Property(f => f.ApprovedById)
            .HasMaxLength(450)
            .HasDefaultValue(null);

        builder.HasOne(f => f.ApprovedBy)
            .WithMany()
            .HasForeignKey(f => f.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.IsApproved)
            .HasDatabaseName("IX_Feedback_IsApproved");

        builder.HasIndex(f => f.CreatedAt)
            .HasDatabaseName("IX_Feedback_CreatedAt");
    }
}
