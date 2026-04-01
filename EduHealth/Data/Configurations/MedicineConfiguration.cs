using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
    {
        public void Configure(EntityTypeBuilder<Medicine> builder)
        {
            builder.ToTable("Medicines");

            builder.HasKey(x => x.MedicineId);

            builder.Property(x => x.MedicineId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Code)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.ActiveIngredient)
                .HasMaxLength(150);

            builder.Property(x => x.Unit)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Packaging)
                .HasMaxLength(255);

            builder.Property(x => x.WarningThreshold)
                .IsRequired();

            builder.Property(x => x.StockQuantity)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Note)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.HasIndex(x => x.Name)
                .IsUnique();
        }
    }
}