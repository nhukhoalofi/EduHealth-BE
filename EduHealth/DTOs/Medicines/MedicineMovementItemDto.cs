namespace EduHealth.DTOs.Medicines
{
    public class MedicineMovementItemDto
    {
        public string MovementId { get; set; } = null!;
        public string Type { get; set; } = null!;
        public int Quantity { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }
        public string? BatchNumber { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string? Reason { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public MedicineMovementCreatedByDto CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
