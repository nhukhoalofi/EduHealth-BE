using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class SystemAlertConfiguration : IEntityTypeConfiguration<SystemAlert>
    {
        public void Configure(EntityTypeBuilder<SystemAlert> builder)
        {
            builder.ToTable("SystemAlerts");

            builder.HasKey(x => x.AlertId);

            builder.Property(x => x.AlertId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.AlertType)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Message)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.Property(x => x.IsRead)
                .IsRequired();
        }
    }
}