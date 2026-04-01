namespace EduHealth.DTOs.Examinations
{
    public class CreateExaminationResponseDto
    {
        public string Id { get; set; } = null!;
        public DateTime VisitDate { get; set; }
        public ExaminationStudentSimpleDto Student { get; set; } = null!;
        public ExaminationUserBriefDto Nurse { get; set; } = null!;
        public ExaminationDiseaseBriefDto? DiseaseType { get; set; }
        public string Symptoms { get; set; } = null!;
        public string Diagnosis { get; set; } = null!;
        public string Treatment { get; set; } = null!;
        public string? Note { get; set; }
        public IReadOnlyList<ExaminationPrescriptionItemDto> Prescriptions { get; set; } = Array.Empty<ExaminationPrescriptionItemDto>();
        public IReadOnlyList<ExaminationInventoryMovementDto> InventoryMovements { get; set; } = Array.Empty<ExaminationInventoryMovementDto>();
        public DateTime CreatedAt { get; set; }
    }
}
