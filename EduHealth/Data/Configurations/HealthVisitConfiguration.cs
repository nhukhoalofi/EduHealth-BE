using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class HealthVisitConfiguration : IEntityTypeConfiguration<HealthVisit>
    {
        public void Configure(EntityTypeBuilder<HealthVisit> builder)
        {
            builder.ToTable("HealthVisits");

            builder.HasKey(x => x.VisitId);

            builder.Property(x => x.VisitId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.StudentUserId)
                .IsRequired();

            builder.Property(x => x.VisitDate)
                .HasColumnType("datetime")
                .IsRequired();

            builder.Property(x => x.Symptoms)
                .HasMaxLength(1000);

            builder.Property(x => x.Diagnosis)
                .HasMaxLength(1000);

            builder.Property(x => x.MeasuredHeight);

            builder.Property(x => x.MeasuredWeight);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.HealthVisits)
                .HasForeignKey(x => x.StudentUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Nurse)
                .WithMany(u => u.HealthVisitsAsNurse)
                .HasForeignKey(x => x.NurseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.DiseaseType)
                .WithMany(d => d.HealthVisits)
                .HasForeignKey(x => x.DiseaseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}