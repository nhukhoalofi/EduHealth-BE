namespace EduHealth.Data.Entities
{
    public class Vaccination
    {
        public int VaccinationId { get; set; }
        public string Name { get; set; } = null!;

        // Navigation
        public ICollection<StudentVaccination> StudentVaccinations { get; set; } = new List<StudentVaccination>();
    }
}