namespace EduHealth.DTOs.Messaging
{
    public class SendMessageRequestDto
    {
        public string? Content { get; set; }
        public string? MessageType { get; set; }
        public string? ClientMessageId { get; set; }
        public IReadOnlyList<int>? AttachmentIds { get; set; }
    }
}
