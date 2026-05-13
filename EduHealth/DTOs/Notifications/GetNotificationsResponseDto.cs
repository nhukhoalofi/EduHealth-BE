namespace EduHealth.DTOs.Notifications
{
    public class GetNotificationsResponseDto
    {
        public IReadOnlyList<NotificationItemDto> Items { get; set; } = Array.Empty<NotificationItemDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int UnreadCount { get; set; }
    }
}
