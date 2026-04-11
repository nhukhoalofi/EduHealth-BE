namespace EduHealth.DTOs.Students
{
    public class StudentDetailDto
    {
        public int UserId { get; set; }
        public string? ImageUrl { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public float CurrentHeight { get; set; }
        public float CurrentWeight { get; set; }
        public string? MedicalHistoryNotes { get; set; }
        public string? Guardian { get; set; }
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Gender { get; set; }
        public bool IsActive { get; set; }
    }
}