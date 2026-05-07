namespace EduHealth.DTOs.Messaging
{
    public class ConversationListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Keyword { get; set; }
    }
}
