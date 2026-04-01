namespace EduHealth.DTOs.Examinations
{
    public class ExaminationListItemDto
    {
        public string Id { get; set; } = null!;
        public DateTime VisitDate { get; set; }
        public ExaminationStudentBriefDto Student { get; set; } = null!;
        public ExaminationUserBriefDto Nurse { get; set; } = null!;
        public ExaminationDiseaseBriefDto? DiseaseType { get; set; }
        public string Symptoms { get; set; } = null!;
        public string Diagnosis { get; set; } = null!;
        public bool HasPrescription { get; set; }
    }
}
