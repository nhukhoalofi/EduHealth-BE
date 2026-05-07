namespace EduHealth.Data.Entities
{
    public class Conversation
    {
        public int ConversationId { get; set; }
        public string ConversationType { get; set; } = "DIRECT";
        public int? StudentUserId { get; set; }
        public string? Title { get; set; }
        public int CreatedByUserId { get; set; }
        public int? LastMessageId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Student? Student { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public ChatMessage? LastMessage { get; set; }
        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
