using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class StudentVaccinationConfiguration : IEntityTypeConfiguration<StudentVaccination>
    {
        public void Configure(EntityTypeBuilder<StudentVaccination> builder)
        {
            builder.ToTable("StudentVaccinations");

            builder.HasKey(x => x.RecordId);

            builder.Property(x => x.RecordId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.CampaignId)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.VaccinatedAt)
                .HasColumnType("date");

            builder.Property(x => x.LotNumber)
                .HasMaxLength(100);

            builder.Property(x => x.Note)
                .HasMaxLength(2000);

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.HasOne(x => x.Student)
                .WithMany(s => s.StudentVaccinations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Vaccination)
                .WithMany(v => v.StudentVaccinations)
                .HasForeignKey(x => x.VaccinationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Campaign)
                .WithMany(c => c.StudentVaccinations)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.CampaignId, x.UserId })
                .IsUnique();
        }
    }
}