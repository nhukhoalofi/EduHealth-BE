namespace EduHealth.Data.Entities
{
    public class ConversationParticipant
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string RoleInConversation { get; set; } = null!;
        public DateTime JoinedAt { get; set; }
        public int? LastReadMessageId { get; set; }
        public DateTime? LastReadAt { get; set; }
        public bool IsPinned { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
