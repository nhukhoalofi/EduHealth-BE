namespace EduHealth.DTOs.Students
{
    public class StudentCreateRequestDto
    {
        public int ClassId { get; set; }
        public string FullName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public float CurrentHeight { get; set; }
        public float CurrentWeight { get; set; }
        public string? MedicalHistoryNotes { get; set; }
        public string? Guardian { get; set; }

        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Gender { get; set; }

        // nếu không truyền thì service dùng mặc định
        public string? Password { get; set; }
    }
}