using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class MedicineStockLogConfiguration : IEntityTypeConfiguration<MedicineStockLog>
    {
        public void Configure(EntityTypeBuilder<MedicineStockLog> builder)
        {
            builder.ToTable("MedicineStockLogs");

            builder.HasKey(x => x.LogId);

            builder.Property(x => x.LogId)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.StockBefore)
                .IsRequired();

            builder.Property(x => x.StockAfter)
                .IsRequired();

            builder.Property(x => x.Reason)
                .HasMaxLength(500)
                ;

            builder.Property(x => x.BatchNumber)
                .HasMaxLength(100);

            builder.Property(x => x.ExpiryDate)
                .HasColumnType("date");

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime")
                .IsRequired();

            builder.Property(x => x.Type)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Note)
                .HasMaxLength(500);

            builder.HasOne(x => x.Medicine)
                .WithMany(m => m.MedicineStockLogs)
                .HasForeignKey(x => x.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                .WithMany(u => u.MedicineStockLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}