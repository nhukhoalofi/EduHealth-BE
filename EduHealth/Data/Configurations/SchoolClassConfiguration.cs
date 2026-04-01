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

            builder.Property(x => x.Code)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Grade)
                .HasMaxLength(20);

            builder.Property(x => x.TeacherName)
                .HasMaxLength(255);

            builder.Property(x => x.TeacherPhone)
                .HasMaxLength(20);

            builder.HasIndex(x => x.Code)
                .IsUnique();
        }
    }
}