namespace EventifyPro.DAL.Configurations;

public class SavedEventConfiguration : IEntityTypeConfiguration<SavedEvent>
{
    public void Configure(EntityTypeBuilder<SavedEvent> builder)
    {
        builder.ToTable("SavedEvents");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasIndex(s => new { s.UserId, s.EventId })
            .IsUnique()
            .HasDatabaseName("IX_SavedEvents_UserId_EventId");

        builder.HasOne(s => s.User)
            .WithMany(u => u.SavedEvents)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Event)
            .WithMany(e => e.SavedEvents)
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(s => !s.Event.IsDeleted);
    }
}
