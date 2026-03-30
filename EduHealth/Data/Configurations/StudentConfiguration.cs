using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("Students");

            builder.HasKey(x => x.UserId);

            builder.Property(x => x.UserId)
                .ValueGeneratedNever();

            builder.Property(x => x.FullName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.DateOfBirth)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.CurrentHeight);

            builder.Property(x => x.CurrentWeight);

            builder.Property(x => x.MedicalHistoryNotes)
                .HasMaxLength(2000);

            builder.Property(x => x.Guardian)
                .HasMaxLength(255);

            builder.Property(x => x.Phone)
                .HasMaxLength(20);

            builder.HasOne(x => x.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
            // GradeId property mapping removed as Grades/GradeId are removed from Classes

            builder.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<Student>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}