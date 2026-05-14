using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications", table =>
            {
                table.HasCheckConstraint("CK_Notifications_Visibility", "[Visibility] IN ('INTERNAL', 'PUBLIC', 'BOTH')");
                table.HasCheckConstraint("CK_Notifications_Status", "[Status] IN ('DRAFT', 'PUBLISHED')");
            });

            builder.HasKey(x => x.NotificationId);

            builder.Property(x => x.NotificationId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.Image)
                .HasMaxLength(500);

            builder.Property(x => x.Type)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Visibility)
                .HasMaxLength(20)
                .HasDefaultValue("INTERNAL")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PUBLISHED")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.Property(x => x.PublishedAt)
                .HasColumnType("datetime");

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Class)
                .WithMany()
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.DiseaseType)
                .WithMany()
                .HasForeignKey(x => x.DiseaseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Vaccination)
                .WithMany()
                .HasForeignKey(x => x.VaccinationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
