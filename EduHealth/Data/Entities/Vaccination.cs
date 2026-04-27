namespace EduHealth.Data.Entities
{
    public class Vaccination
    {
        public int VaccinationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<StudentVaccination> StudentVaccinations { get; set; } = new List<StudentVaccination>();
    }
}