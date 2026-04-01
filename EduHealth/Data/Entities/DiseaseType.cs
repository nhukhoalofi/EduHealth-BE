namespace EduHealth.Data.Entities
{
    public class DiseaseType
    {
        public int DiseaseId { get; set; }
        public string Code { get; set; } = null!;
        public string DiseaseName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsContagious { get; set; }
        public string? StandardTreatment { get; set; }

        // Navigation
        public ICollection<HealthVisit> HealthVisits { get; set; } = new List<HealthVisit>();
    }
}