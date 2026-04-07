using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class VaccinationCampaignConfiguration : IEntityTypeConfiguration<VaccinationCampaign>
    {
        public void Configure(EntityTypeBuilder<VaccinationCampaign> builder)
        {
            builder.ToTable("VaccinationCampaigns");

            builder.HasKey(x => x.CampaignId);

            builder.Property(x => x.CampaignId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Code)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.VaccineName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.DoseNumber)
                .IsRequired();

            builder.Property(x => x.ScheduledDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.TargetType)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Note)
                .HasMaxLength(2000);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.HasOne(x => x.CreatedByUser)
                .WithMany(u => u.VaccinationCampaigns)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Code)
                .IsUnique();
        }
    }
}
