namespace EduHealth.Data.Entities
{
    public class StudentVaccination
    {
        public int RecordId { get; set; }
        public int UserId { get; set; }
        public int CampaignId { get; set; }
        public int VaccinationId { get; set; }
        public string Status { get; set; } = null!;

        public DateOnly? VaccinatedAt { get; set; }
        public string? LotNumber { get; set; }
        public string? Note { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public Student Student { get; set; } = null!;
        public Vaccination Vaccination { get; set; } = null!;
        public VaccinationCampaign Campaign { get; set; } = null!;
    }
}