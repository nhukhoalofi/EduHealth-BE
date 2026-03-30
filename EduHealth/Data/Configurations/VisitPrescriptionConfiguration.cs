using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class VisitPrescriptionConfiguration : IEntityTypeConfiguration<VisitPrescription>
    {
        public void Configure(EntityTypeBuilder<VisitPrescription> builder)
        {
            builder.ToTable("VisitPrescriptions");

            builder.HasKey(x => x.PrescriptionId);

            builder.Property(x => x.PrescriptionId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.UsageIns)
                .HasMaxLength(500);

            builder.HasOne(x => x.HealthVisit)
                .WithMany(v => v.VisitPrescriptions)
                .HasForeignKey(x => x.VisitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Medicine)
                .WithMany(m => m.VisitPrescriptions)
                .HasForeignKey(x => x.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}