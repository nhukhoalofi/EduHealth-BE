namespace EduHealth.DTOs.Messaging
{
    public class MessageItemDto
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string SenderRole { get; set; } = null!;
        public string? SenderAvatarUrl { get; set; }
        public string Content { get; set; } = null!;
        public string MessageType { get; set; } = null!;
        public string? ClientMessageId { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsMine { get; set; }
        public IReadOnlyList<ConversationReadDto> ReadBy { get; set; } = Array.Empty<ConversationReadDto>();
        public IReadOnlyList<MessageAttachmentDto> Attachments { get; set; } = Array.Empty<MessageAttachmentDto>();
    }

    public class MessageAttachmentDto
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = null!;
        public string? OriginalFileName { get; set; }
        public string FileUrl { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long SizeBytes { get; set; }
    }
}
