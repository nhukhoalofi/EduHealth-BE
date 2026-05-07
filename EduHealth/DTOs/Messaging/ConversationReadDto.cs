namespace EduHealth.DTOs.Messaging
{
    public class ConversationReadDto
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int? LastReadMessageId { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
