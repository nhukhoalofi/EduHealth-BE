namespace EduHealth.DTOs.Messaging
{
    public class MessagingErrorDto
    {
        public string Code { get; set; } = null!;
        public string Message { get; set; } = null!;
        public int? ConversationId { get; set; }
        public string? ClientMessageId { get; set; }
    }
}
