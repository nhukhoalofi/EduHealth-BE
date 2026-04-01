namespace EduHealth.DTOs.Examinations
{
    public class ExaminationPrescriptionItemDto
    {
        public string PrescriptionId { get; set; } = null!;
        public string MedicineId { get; set; } = null!;
        public string MedicineName { get; set; } = null!;
        public int Quantity { get; set; }
        public string? Dosage { get; set; }
        public string? UsageInstruction { get; set; }
    }
}
