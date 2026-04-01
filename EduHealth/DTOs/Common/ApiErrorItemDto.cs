namespace EduHealth.DTOs.Common
{
    public class ApiErrorItemDto
    {
        public string Field { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
