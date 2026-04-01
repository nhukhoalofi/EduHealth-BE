namespace EduHealth.DTOs.Examinations
{
    public class CreateExaminationRequestDto
    {
        public string StudentId { get; set; } = null!;
        public DateTime VisitDate { get; set; }
        public string? DiseaseTypeId { get; set; }
        public string Symptoms { get; set; } = null!;
        public string Diagnosis { get; set; } = null!;
        public string Treatment { get; set; } = null!;
        public string? Note { get; set; }
        public IReadOnlyList<CreateExaminationPrescriptionItemDto>? Prescriptions { get; set; }
    }
}
