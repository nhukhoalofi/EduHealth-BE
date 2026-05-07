using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
    {
        public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
        {
            builder.ToTable("ConversationParticipants");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.RoleInConversation)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.JoinedAt)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.Property(x => x.LastReadAt)
                .HasColumnType("datetime2");

            builder.Property(x => x.IsPinned)
                .HasDefaultValue(false);

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ConversationId, x.UserId })
                .IsUnique();

            builder.HasIndex(x => new { x.UserId, x.ConversationId });
        }
    }
}
