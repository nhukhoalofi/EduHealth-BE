using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class AllergyTypeConfiguration : IEntityTypeConfiguration<AllergyType>
    {
        public void Configure(EntityTypeBuilder<AllergyType> builder)
        {
            builder.ToTable("AllergyTypes");

            builder.HasKey(x => x.AllergyId);

            builder.Property(x => x.AllergyId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.AllergyName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Severity)
                .HasMaxLength(50)
                .IsRequired();
        }
    }
}