namespace EduHealth.DTOs.Examinations
{
    public class ExaminationInventoryMovementDto
    {
        public string MovementId { get; set; } = null!;
        public string MedicineId { get; set; } = null!;
        public string? BatchId { get; set; }
        public string Type { get; set; } = null!;
        public int Quantity { get; set; }
    }
}
