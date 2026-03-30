using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
    {
        public void Configure(EntityTypeBuilder<SchoolClass> builder)
        {
            builder.ToTable("Classes");

            builder.HasKey(x => x.ClassId);

            builder.Property(x => x.ClassId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.ClassName)
                .HasMaxLength(50)
                .IsRequired();
        }
    }
}