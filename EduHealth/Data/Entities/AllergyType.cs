namespace EduHealth.Data.Entities
{
    public class AllergyType
    {
        public int AllergyId { get; set; }
        public string AllergyName { get; set; } = null!;
        public string Severity { get; set; } = null!;

        // Navigation
        public ICollection<StudentAllergy> StudentAllergies { get; set; } = new List<StudentAllergy>();
    }
}