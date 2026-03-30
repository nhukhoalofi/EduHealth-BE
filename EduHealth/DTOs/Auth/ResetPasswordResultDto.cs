namespace EduHealth.DTOs.Auth
{
    public class ResetPasswordResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string? Field { get; set; }
    }
}
