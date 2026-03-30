namespace EduHealth.DTOs.Auth
{
    public class MeResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public string? Avatar { get; set; }
    }
}