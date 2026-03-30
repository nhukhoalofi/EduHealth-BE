namespace EduHealth.Data.Entities
{
    public class VisitPrescription
    {
        public int PrescriptionId { get; set; }
        public int VisitId { get; set; }
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public string? UsageIns { get; set; }

        // Navigation
        public HealthVisit HealthVisit { get; set; } = null!;
        public Medicine Medicine { get; set; } = null!;
    }
}