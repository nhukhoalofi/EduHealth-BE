using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
    {
        public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
        {
            builder.ToTable("NotificationRecipients");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.IsRead)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20);

            builder.Property(x => x.SentAt)
                .HasColumnType("datetime");

            builder.Property(x => x.ReadAt)
                .HasColumnType("datetime");

            builder.HasOne(x => x.Notification)
                .WithMany(n => n.Recipients)
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.NotificationId, x.UserId })
                .IsUnique();
        }
    }
}
