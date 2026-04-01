namespace EduHealth.Data.Entities
{
    public class Medicine
    {
        public int MedicineId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ActiveIngredient { get; set; }
        public string Unit { get; set; } = null!;
        public string? Packaging { get; set; }
        public int WarningThreshold { get; set; }
        public int StockQuantity { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ICollection<VisitPrescription> VisitPrescriptions { get; set; } = new List<VisitPrescription>();
        public ICollection<MedicineStockLog> MedicineStockLogs { get; set; } = new List<MedicineStockLog>();
    }
}