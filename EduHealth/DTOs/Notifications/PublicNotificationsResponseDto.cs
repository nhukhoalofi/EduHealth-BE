namespace EduHealth.DTOs.Notifications
{
    public class PublicNotificationsResponseDto
    {
        public IReadOnlyList<PublicNotificationItemDto> Items { get; set; } = Array.Empty<PublicNotificationItemDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
