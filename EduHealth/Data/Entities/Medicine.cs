namespace EduHealth.Data.Entities
{
    public class Medicine
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = null!;
        public int StockQuantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? Unit { get; set; }
        public int? MinStockLevel { get; set; }

        // Navigation
        public ICollection<VisitPrescription> VisitPrescriptions { get; set; } = new List<VisitPrescription>();
        public ICollection<MedicineStockLog> MedicineStockLogs { get; set; } = new List<MedicineStockLog>();
    }
}