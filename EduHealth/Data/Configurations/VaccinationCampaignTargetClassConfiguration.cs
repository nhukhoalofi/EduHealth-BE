using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class VaccinationCampaignTargetClassConfiguration : IEntityTypeConfiguration<VaccinationCampaignTargetClass>
    {
        public void Configure(EntityTypeBuilder<VaccinationCampaignTargetClass> builder)
        {
            builder.ToTable("VaccinationCampaignTargetClasses");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CampaignId)
                .IsRequired();

            builder.Property(x => x.ClassId)
                .IsRequired();

            builder.HasOne(x => x.Campaign)
                .WithMany(c => c.TargetClasses)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Class)
                .WithMany()
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.CampaignId, x.ClassId })
                .IsUnique();
        }
    }
}
