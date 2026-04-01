namespace EduHealth.DTOs.Users
{
    public class UserListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Keyword { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
    }
}
