using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class DiseaseTypeConfiguration : IEntityTypeConfiguration<DiseaseType>
    {
        public void Configure(EntityTypeBuilder<DiseaseType> builder)
        {
            builder.ToTable("DiseaseType");

            builder.HasKey(x => x.DiseaseId);

            builder.Property(x => x.DiseaseId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.DiseaseName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.StandardTreatment)
                .HasMaxLength(1000);
        }
    }
}