using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.ToTable("Conversations");

            builder.HasKey(x => x.ConversationId);

            builder.Property(x => x.ConversationId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.ConversationType)
                .HasMaxLength(20)
                .HasDefaultValue("DIRECT")
                .IsRequired();

            builder.Property(x => x.Title)
                .HasMaxLength(255);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.LastMessage)
                .WithMany()
                .HasForeignKey(x => x.LastMessageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
