namespace EduHealth.DTOs.Users
{
    public class ResetPasswordResponseDto
    {
        public string Id { get; set; } = null!;
        public string ResetMode { get; set; } = null!;
        public string? TemporaryPassword { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
