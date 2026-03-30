namespace EduHealth.Data.Entities
{
    public class StudentAllergy
    {
        public int RecordId { get; set; }
        public int UserId { get; set; }
        public int AllergyId { get; set; }
        public string? Note { get; set; }

        // Navigation
        public Student Student { get; set; } = null!;
        public AllergyType AllergyType { get; set; } = null!;
    }
}