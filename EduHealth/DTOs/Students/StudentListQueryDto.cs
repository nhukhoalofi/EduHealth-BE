namespace EduHealth.DTOs.Students
{
    public class StudentListQueryDto
    {
        public string? Search { get; set; }
        public int? ClassId { get; set; }
        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}