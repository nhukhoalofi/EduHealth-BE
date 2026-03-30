namespace EduHealth.Data.Entities
{
    public class StudentVaccination
    {
        public int RecordId { get; set; }
        public int UserId { get; set; }
        public int VaccinationId { get; set; }
        public string Status { get; set; } = null!;

        // Navigation
        public Student Student { get; set; } = null!;
        public Vaccination Vaccination { get; set; } = null!;
    }
}