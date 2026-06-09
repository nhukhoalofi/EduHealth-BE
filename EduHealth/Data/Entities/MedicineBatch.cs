namespace EduHealth.Data.Entities
{
    public class MedicineBatch
    {
        public int MedicineBatchId { get; set; }
        public string Code { get; set; } = null!;
        public int MedicineId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public int InitialQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public string? Note { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Medicine Medicine { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public ICollection<MedicineStockLog> StockLogs { get; set; } = new List<MedicineStockLog>();
    }
}
