using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class PasswordResetOtpConfiguration : IEntityTypeConfiguration<PasswordResetOtp>
    {
        public void Configure(EntityTypeBuilder<PasswordResetOtp> builder)
        {
            builder.ToTable("PasswordResetOtps");

            builder.HasKey(x => x.PasswordResetOtpId);

            builder.Property(x => x.PasswordResetOtpId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.OtpCode)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.ResetToken)
                .HasMaxLength(200);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.Property(x => x.OtpExpiresAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.Property(x => x.VerifiedAt)
                .HasColumnType("datetime");

            builder.Property(x => x.ResetTokenExpiresAt)
                .HasColumnType("datetime");

            builder.HasOne(x => x.User)
                .WithMany(u => u.PasswordResetOtps)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.OtpCode });
            builder.HasIndex(x => x.ResetToken);
        }
    }
}