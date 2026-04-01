namespace EduHealth.DTOs.Medicines
{
    public class UpdateMedicineStatusRequestDto
    {
        public string Status { get; set; } = null!;
        public string? Reason { get; set; }
    }
}
