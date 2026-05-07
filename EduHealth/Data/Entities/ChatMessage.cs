namespace EduHealth.Data.Entities
{
    public class ChatMessage
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = null!;
        public string MessageType { get; set; } = "TEXT";
        public string? ClientMessageId { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;
        public ICollection<ChatMessageAttachment> Attachments { get; set; } = new List<ChatMessageAttachment>();
    }
}
