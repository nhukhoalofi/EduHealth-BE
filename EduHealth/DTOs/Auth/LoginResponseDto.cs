namespace EduHealth.DTOs.Auth
{
    public class LoginResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? Avatar { get; set; }
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}