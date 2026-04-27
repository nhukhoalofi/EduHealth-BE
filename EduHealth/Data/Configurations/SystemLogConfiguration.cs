using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public sealed class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
    {
        public void Configure(EntityTypeBuilder<SystemLog> builder)
        {
            builder.ToTable("SystemLogs");

            builder.HasKey(x => x.LogId);
            builder.Property(x => x.LogId).ValueGeneratedOnAdd();

            builder.Property(x => x.CreatedAt).IsRequired();

            builder.Property(x => x.ActorName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.ActorUsername).HasMaxLength(255);
            builder.Property(x => x.ActorRole).HasMaxLength(50).IsRequired();

            builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Action).HasMaxLength(100).IsRequired();

            builder.Property(x => x.TargetType).HasMaxLength(100).IsRequired();
            builder.Property(x => x.TargetId).HasMaxLength(100);
            builder.Property(x => x.TargetLabel).HasMaxLength(255).IsRequired();

            builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();

            builder.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.ActorRole);
            builder.HasIndex(x => x.Module);
            builder.HasIndex(x => x.Action);

            builder.HasOne(x => x.ActorUser)
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
