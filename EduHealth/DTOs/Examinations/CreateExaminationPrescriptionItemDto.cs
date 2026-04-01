namespace EduHealth.DTOs.Examinations
{
    public class CreateExaminationPrescriptionItemDto
    {
        public string MedicineId { get; set; } = null!;
        public int Quantity { get; set; }
        public string? Dosage { get; set; }
        public string? UsageInstruction { get; set; }
    }
}
