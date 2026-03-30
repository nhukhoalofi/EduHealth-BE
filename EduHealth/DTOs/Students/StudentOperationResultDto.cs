namespace EduHealth.DTOs.Students
{
    public class StudentOperationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string? Field { get; set; }
    }

    public class StudentCreateResultDto : StudentOperationResultDto
    {
        public StudentDetailDto Data { get; set; } = null!;
    }

    public class StudentListResultDto
    {
        public IReadOnlyList<StudentListItemDto> Items { get; set; } = Array.Empty<StudentListItemDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}