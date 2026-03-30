namespace EduHealth.Data.Entities
{
    public class HealthVisit
    {
        public int VisitId { get; set; }
        public int StudentUserId { get; set; }
        public int NurseId { get; set; }
        public DateTime VisitDate { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public float MeasuredHeight { get; set; }
        public float MeasuredWeight { get; set; }
        public int? DiseaseId { get; set; }

        // Navigation
        public Student Student { get; set; } = null!;
        public User Nurse { get; set; } = null!;
        public DiseaseType? DiseaseType { get; set; }
        public ICollection<VisitPrescription> VisitPrescriptions { get; set; } = new List<VisitPrescription>();
    }
}