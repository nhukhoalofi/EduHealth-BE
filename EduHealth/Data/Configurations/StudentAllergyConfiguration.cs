using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class StudentAllergyConfiguration : IEntityTypeConfiguration<StudentAllergy>
    {
        public void Configure(EntityTypeBuilder<StudentAllergy> builder)
        {
            builder.ToTable("StudentAllergies");

            builder.HasKey(x => x.RecordId);

            builder.Property(x => x.RecordId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Note)
                .HasMaxLength(500);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.StudentAllergies)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.AllergyType)
                .WithMany(a => a.StudentAllergies)
                .HasForeignKey(x => x.AllergyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.UserId, x.AllergyId })
                .IsUnique();
        }
    }
}