namespace EduHealth.Data.Entities
{
    public class Student
    {
        public int UserId { get; set; }
        public string Code { get; set; } = null!;
        public int ClassId { get; set; }
        public string FullName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public float CurrentHeight { get; set; }
        public float CurrentWeight { get; set; }
        public string? MedicalHistoryNotes { get; set; }
        public string? Guardian { get; set; }
        public string? Phone { get; set; }

        // Navigation
        public SchoolClass Class { get; set; } = null!;
        public User User { get; set; } = null!;

        public ICollection<StudentAllergy> StudentAllergies { get; set; } = new List<StudentAllergy>();
        public ICollection<StudentVaccination> StudentVaccinations { get; set; } = new List<StudentVaccination>();
        public ICollection<HealthVisit> HealthVisits { get; set; } = new List<HealthVisit>();
    }
}