namespace EduHealth.DTOs.Messaging
{
    public class ConversationParticipantDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }
}
