namespace EduHealth.DTOs.Messaging
{
    public class MessagingContactDto
    {
        public int UserId { get; set; }
        public int? StudentId { get; set; }
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? ClassName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool HasConversation { get; set; }
        public int? ConversationId { get; set; }
        public DateTime? LastMessageAt { get; set; }
    }
}
