namespace EduHealth.DTOs.Auth
{
    public class ResetPasswordRequestDto
    {
        public string Email { get; set; } = null!;
        public string ResetToken { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}