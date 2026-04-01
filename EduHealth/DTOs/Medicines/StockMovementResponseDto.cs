namespace EduHealth.DTOs.Medicines
{
    public class StockMovementResponseDto
    {
        public string MedicineId { get; set; } = null!;
        public string MovementId { get; set; } = null!;
        public string Type { get; set; } = null!;
        public int Quantity { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
