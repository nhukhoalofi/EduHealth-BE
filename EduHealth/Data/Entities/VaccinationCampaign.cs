namespace EduHealth.Data.Entities
{
    public class VaccinationCampaign
    {
        public int CampaignId { get; set; }
        public string Code { get; set; } = null!; // VACxxx

        public string Name { get; set; } = null!;
        public string VaccineName { get; set; } = null!;
        public int DoseNumber { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string TargetType { get; set; } = null!; // CLASS | STUDENT
        public string Status { get; set; } = "ACTIVE"; // ACTIVE | COMPLETED | CANCELLED
        public string? Note { get; set; }

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User CreatedByUser { get; set; } = null!;
        public ICollection<VaccinationCampaignTargetClass> TargetClasses { get; set; } = new List<VaccinationCampaignTargetClass>();
        public ICollection<StudentVaccination> StudentVaccinations { get; set; } = new List<StudentVaccination>();
    }
}
