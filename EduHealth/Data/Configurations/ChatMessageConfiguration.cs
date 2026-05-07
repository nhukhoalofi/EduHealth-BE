using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");

            builder.HasKey(x => x.MessageId);

            builder.Property(x => x.MessageId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.MessageType)
                .HasMaxLength(20)
                .HasDefaultValue("TEXT")
                .IsRequired();

            builder.Property(x => x.ClientMessageId)
                .HasMaxLength(100);

            builder.Property(x => x.SentAt)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.Property(x => x.EditedAt)
                .HasColumnType("datetime2");

            builder.Property(x => x.DeletedAt)
                .HasColumnType("datetime2");

            builder.Property(x => x.IsDeleted)
                .HasDefaultValue(false);

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ConversationId, x.SentAt })
                .IsDescending(false, true);

            builder.HasIndex(x => new { x.ConversationId, x.MessageId })
                .IsDescending(false, true);

            builder.HasIndex(x => new { x.SenderUserId, x.SentAt })
                .IsDescending(false, true);

            builder.HasIndex(x => new { x.ConversationId, x.SenderUserId, x.ClientMessageId })
                .IsUnique()
                .HasFilter("[ClientMessageId] IS NOT NULL");
        }
    }
}
