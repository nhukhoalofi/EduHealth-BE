namespace EduHealth.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string Identifier { get; set; } = null!; // email hoặc phone
        public string Password { get; set; } = null!;
    }
}