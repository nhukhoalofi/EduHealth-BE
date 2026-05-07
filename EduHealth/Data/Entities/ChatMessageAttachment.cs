namespace EduHealth.Data.Entities
{
    public class ChatMessageAttachment
    {
        public int AttachmentId { get; set; }
        public int MessageId { get; set; }
        public string FileName { get; set; } = null!;
        public string? OriginalFileName { get; set; }
        public string FileUrl { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long SizeBytes { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime UploadedAt { get; set; }

        public ChatMessage Message { get; set; } = null!;
        public User UploadedByUser { get; set; } = null!;
    }
}
