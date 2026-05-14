namespace EduHealth.DTOs.Notifications
{
    public class SentNotificationItemDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Image { get; set; }
        public string Type { get; set; } = null!;
        public string Visibility { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int? ClassId { get; set; }
        public int? DiseaseId { get; set; }
        public int? VaccinationId { get; set; }
        public int TotalRecipients { get; set; }
        public int ReadCount { get; set; }
        public int UnreadCount { get; set; }
    }
}
