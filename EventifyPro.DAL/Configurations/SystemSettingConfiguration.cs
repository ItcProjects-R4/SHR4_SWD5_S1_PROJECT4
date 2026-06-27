namespace EventifyPro.DAL.Configurations
{
    public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
    {
        public void Configure(EntityTypeBuilder<SystemSetting> builder)
        {
            builder.ToTable("SystemSettings");

            builder.HasKey(s => s.Key);

            builder.Property(s => s.Key)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(s => s.Value)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(s => s.UpdatedAt)
                .HasDefaultValue(null);
        }
    }
}
