namespace EventifyPro.DAL.Configurations;

/// <summary>
/// Entity Framework configuration for the Review entity.
/// </summary>
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    /// <summary>
    /// Configures the Review entity in the model.
    /// </summary>
    /// <param name="builder">The builder for configuring the entity type.</param>
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Reviews_Rating",
                "Rating BETWEEN 1 AND 5");
        });

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.Property(r => r.IsHidden)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasQueryFilter(r => !r.IsHidden && !r.Event.IsDeleted);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(r => r.UpdatedAt)
            .HasDefaultValue(null);

        builder.Property(r => r.HiddenById)
            .HasMaxLength(450)
            .HasDefaultValue(null);

        builder.Property(r => r.HiddenReason)
            .HasMaxLength(500)
            .HasDefaultValue(null);

        builder.HasOne(r => r.HiddenBy)
            .WithMany()
            .HasForeignKey(r => r.HiddenById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Event)
            .WithMany(e => e.Reviews)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.UserId, r.EventId })
            .IsUnique()
            .HasDatabaseName("IX_Reviews_UserId_EventId");

        builder.HasIndex(r => r.EventId)
            .HasDatabaseName("IX_Reviews_EventId");
    }
}
