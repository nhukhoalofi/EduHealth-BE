using Microsoft.AspNetCore.Http;

namespace EduHealth.DTOs.Students
{
    public class StudentImportRequestDto
    {
        public IFormFile File { get; set; } = null!;
    }
}