namespace EduHealth.Data.Entities
{
    public class SchoolClass
    {
        public int ClassId { get; set; }
        public string Code { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string? Grade { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherPhone { get; set; }

        // Navigation
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}