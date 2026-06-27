namespace EventifyPro.DAL.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Type)
            .IsRequired();

        builder.Property(n => n.IsRead)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.RedirectUrl)
            .HasMaxLength(250);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // One-to-many relationship with ApplicationUser
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
