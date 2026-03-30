namespace EduHealth.DTOs.Students
{
    public class StudentUpdateRequestDto
    {
        public int? ClassId { get; set; }
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public float? CurrentHeight { get; set; }
        public float? CurrentWeight { get; set; }
        public string? MedicalHistoryNotes { get; set; }
        public string? Guardian { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
    }
}