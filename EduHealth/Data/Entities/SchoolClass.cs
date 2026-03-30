namespace EduHealth.Data.Entities
{
    public class SchoolClass
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;

        // Navigation
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}