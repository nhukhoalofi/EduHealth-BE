namespace EduHealth.DTOs.Auth
{
    public class MeResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public string Status { get; set; } = null!;
        public string? Avatar { get; set; }
        public string? Gender { get; set; }
        public StudentPersonalProfileDto? StudentProfile { get; set; }
    }

    public class StudentPersonalProfileDto
    {
        public string StudentId { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string? Grade { get; set; }
        public float CurrentHeight { get; set; }
        public float CurrentWeight { get; set; }
        public string? Guardian { get; set; }
        public string? GuardianPhone { get; set; }
        public string? MedicalHistoryNotes { get; set; }
    }
}
