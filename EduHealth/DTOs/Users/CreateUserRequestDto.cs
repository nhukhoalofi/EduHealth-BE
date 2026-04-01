namespace EduHealth.DTOs.Users
{
    public class CreateUserRequestDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
    }
}
