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

            builder.Property(x => x.MedicineName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.StockQuantity)
                .IsRequired();

            builder.Property(x => x.ExpiryDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.Unit)
                .HasMaxLength(50);

            builder.Property(x => x.MinStockLevel);
        }
    }
}