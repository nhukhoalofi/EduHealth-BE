namespace EduHealth.DTOs.Notifications
{
    public class SentNotificationsResponseDto
    {
        public IReadOnlyList<SentNotificationItemDto> Items { get; set; } = Array.Empty<SentNotificationItemDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
