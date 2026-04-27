using Microsoft.AspNetCore.Http;

namespace EduHealth.DTOs.Auth
{
    public class UpdateAvatarRequestDto
    {
        public IFormFile File { get; set; } = null!;
    }
}