namespace EduHealth.DTOs.Notifications
{
    public class NotificationRecipientsPreviewResponseDto
    {
        public int Total { get; set; }
        public IReadOnlyList<NotificationRecipientPreviewItemDto> Recipients { get; set; } = Array.Empty<NotificationRecipientPreviewItemDto>();
    }
}
