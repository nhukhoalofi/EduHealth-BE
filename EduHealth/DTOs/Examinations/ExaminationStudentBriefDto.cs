namespace EduHealth.DTOs.Examinations
{
    public class ExaminationStudentBriefDto
    {
        public string StudentId { get; set; } = null!;
        public string StudentCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
    }
}
