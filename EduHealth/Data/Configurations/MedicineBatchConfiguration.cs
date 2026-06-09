using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduHealth.Data.Configurations
{
    public class MedicineBatchConfiguration : IEntityTypeConfiguration<MedicineBatch>
    {
        public void Configure(EntityTypeBuilder<MedicineBatch> builder)
        {
            builder.ToTable("MedicineBatches");

            builder.HasKey(x => x.MedicineBatchId);
            builder.Property(x => x.MedicineBatchId).ValueGeneratedOnAdd();

            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.BatchNumber).HasMaxLength(100);
            builder.Property(x => x.ReceivedAt).HasColumnType("datetime").IsRequired();
            builder.Property(x => x.ExpiryDate).HasColumnType("date").IsRequired();
            builder.Property(x => x.InitialQuantity).IsRequired();
            builder.Property(x => x.RemainingQuantity).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.CreatedAt).HasColumnType("datetime").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime").IsRequired();

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => new { x.MedicineId, x.ExpiryDate, x.Status });

            builder.HasOne(x => x.Medicine)
                .WithMany(x => x.MedicineBatches)
                .HasForeignKey(x => x.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
