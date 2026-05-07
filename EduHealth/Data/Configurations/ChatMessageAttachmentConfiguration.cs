using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class ChatMessageAttachmentConfiguration : IEntityTypeConfiguration<ChatMessageAttachment>
    {
        public void Configure(EntityTypeBuilder<ChatMessageAttachment> builder)
        {
            builder.ToTable("ChatMessageAttachments");

            builder.HasKey(x => x.AttachmentId);

            builder.Property(x => x.AttachmentId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.FileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.OriginalFileName)
                .HasMaxLength(255);

            builder.Property(x => x.FileUrl)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.UploadedAt)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.HasOne(x => x.Message)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
