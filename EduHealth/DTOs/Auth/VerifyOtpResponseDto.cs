namespace EduHealth.DTOs.Auth
{
    public class VerifyOtpResponseDto
    {
        public string ResetToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}