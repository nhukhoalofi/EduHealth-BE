namespace EduHealth.Data.Entities
{
    public class MedicineStockLog
    {
        public int LogId { get; set; }
        public int MedicineId { get; set; }
        public int UserId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; } = null!;
        public int? VisitId { get; set; }
        public string? Note { get; set; }

        // Navigation
        public Medicine Medicine { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}