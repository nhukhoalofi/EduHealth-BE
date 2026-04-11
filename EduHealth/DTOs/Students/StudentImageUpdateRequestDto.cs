using Microsoft.AspNetCore.Http;

namespace EduHealth.DTOs.Students
{
    public class StudentImageUpdateRequestDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
