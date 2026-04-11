namespace EduHealth.DTOs.Students
{
    public class StudentListItemDto
    {
        public int UserId { get; set; }
        public string? ImageUrl { get; set; }
        public string FullName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Guardian { get; set; }
        public float CurrentHeight { get; set; }
        public float CurrentWeight { get; set; }
        public bool IsActive { get; set; }
    }
}