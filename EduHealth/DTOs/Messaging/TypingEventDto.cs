namespace EduHealth.DTOs.Messaging
{
    public class TypingEventDto
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsTyping { get; set; }
        public DateTime SentAt { get; set; }
    }
}
