namespace EduHealth.DTOs.Messaging
{
    public class MessageListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
        public int? BeforeMessageId { get; set; }
    }
}
