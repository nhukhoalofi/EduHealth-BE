namespace EduHealth.DTOs.Notifications
{
    public class NotificationRecipientPreviewItemDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
    }
}
