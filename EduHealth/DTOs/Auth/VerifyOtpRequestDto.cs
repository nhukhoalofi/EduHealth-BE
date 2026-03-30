namespace EduHealth.DTOs.Auth
{
    public class VerifyOtpRequestDto
    {
        public string Email { get; set; } = null!;
        public string Otp { get; set; } = null!;
    }
}
