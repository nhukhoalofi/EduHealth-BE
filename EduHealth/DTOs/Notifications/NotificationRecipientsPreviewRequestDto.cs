namespace EduHealth.DTOs.Notifications
{
    public class NotificationRecipientsPreviewRequestDto
    {
        public int? ClassId { get; set; }
        public IReadOnlyList<int>? UserIds { get; set; }
    }
}
