namespace EduHealth.DTOs.Messaging
{
    public class ConversationDetailDto
    {
        public int ConversationId { get; set; }
        public string ConversationType { get; set; } = null!;
        public string? Title { get; set; }
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? ClassName { get; set; }
        public string? AvatarUrl { get; set; }
        public IReadOnlyList<ConversationParticipantDto> Participants { get; set; } = Array.Empty<ConversationParticipantDto>();
        public MessageItemDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public bool IsPinned { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
