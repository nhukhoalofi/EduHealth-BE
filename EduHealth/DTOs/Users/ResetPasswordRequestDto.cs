namespace EduHealth.DTOs.Users
{
    public class ResetPasswordRequestDto
    {
        public string Mode { get; set; } = null!;
        public string? NewPassword { get; set; }
    }
}
