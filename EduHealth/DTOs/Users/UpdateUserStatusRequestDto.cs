namespace EduHealth.DTOs.Users
{
    public class UpdateUserStatusRequestDto
    {
        public string Status { get; set; } = null!;
        public string? Reason { get; set; }
    }
}
