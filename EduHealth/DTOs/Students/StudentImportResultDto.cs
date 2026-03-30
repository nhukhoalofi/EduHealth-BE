namespace EduHealth.DTOs.Students
{
    public class StudentImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount => Errors.Count;
        public List<StudentImportErrorDto> Errors { get; set; } = new();
    }

    public class StudentImportErrorDto
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = null!;
    }
}